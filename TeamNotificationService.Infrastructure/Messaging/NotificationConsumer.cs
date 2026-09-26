using System.Text;
using System.Text.Json;
using HandlebarsDotNet;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TeamNotificationService.Application.Interfaces.Messaging;
using TeamNotificationService.Application.Interfaces.Repositories;
using TeamNotificationService.Application.Interfaces.Services.Emails;
using TeamNotificationService.Application.Interfaces.Services.NotificationDeliveries;
using TeamNotificationService.Application.Models.Emails;
using TeamNotificationService.Application.Models.Notifications;
using TeamNotificationService.Domain.Entities;
using TeamNotificationService.Domain.Enums;
using TeamNotificationService.Domain.Exceptions;
using TeamNotificationService.Domain.Utils;
using TeamNotificationService.Infrastructure.Options;

namespace TeamNotificationService.Infrastructure.Messaging;

/// <summary>
/// Processes every CloudEvent whose data uses CreateNotificationRequestModel.
/// Declares its organization-event, retry, and dead-letter topology once at
/// startup, then consumes the configured queue and publishes configured retries.
/// </summary>
public sealed class NotificationConsumer : BackgroundService
{
    private readonly RabbitMqConnection _rabbitMqConnection;
    private readonly NotificationPersistence _notificationPersistence;
    private readonly NotificationBellSender _notificationBellSender;
    private readonly NotificationEmailSender _notificationEmailSender;
    private readonly RabbitMqOptions _options;
    private readonly Serilog.ILogger _logger;

    public NotificationConsumer(
        RabbitMqConnection rabbitMqConnection,
        NotificationPersistence notificationPersistence,
        NotificationBellSender notificationBellSender,
        NotificationEmailSender notificationEmailSender,
        IOptions<RabbitMqOptions> options,
        Serilog.ILogger logger)
    {
        _rabbitMqConnection = rabbitMqConnection;
        _notificationPersistence = notificationPersistence;
        _notificationBellSender = notificationBellSender;
        _notificationEmailSender = notificationEmailSender;
        _options = options.Value;
        _logger = logger;
    }

    private async Task ConfigureTopologyAsync(IChannel channel, CancellationToken cancellationToken)
    {
        var topology = _options.Topology;
        var organizationService = topology.OrganizationService;

        await channel.ExchangeDeclareAsync(
            organizationService.Exchange,
            ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(
            topology.DeadLetterExchange,
            ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(
            topology.RetryExchange,
            ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(
            topology.RetryReturnExchange,
            ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            topology.DeadLetterQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            topology.DeadLetterQueue,
            topology.DeadLetterExchange,
            _options.Consumer.Queue,
            arguments: null,
            noWait: false,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            _options.Consumer.Queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = topology.DeadLetterExchange,
                ["x-dead-letter-routing-key"] = _options.Consumer.Queue
            },
            cancellationToken: cancellationToken);

        foreach (var routingKey in organizationService.RoutingKeys)
        {
            await channel.QueueBindAsync(
                _options.Consumer.Queue,
                organizationService.Exchange,
                routingKey,
                arguments: null,
                noWait: false,
                cancellationToken: cancellationToken);
        }

        await channel.QueueBindAsync(
            _options.Consumer.Queue,
            topology.RetryReturnExchange,
            _options.Consumer.Queue,
            arguments: null,
            noWait: false,
            cancellationToken: cancellationToken);

        foreach (var retry in topology.RetryQueues)
        {
            await channel.QueueDeclareAsync(
                retry.Queue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: new Dictionary<string, object?>
                {
                    ["x-message-ttl"] = retry.MessageTtlMilliseconds,
                    ["x-dead-letter-exchange"] = topology.RetryReturnExchange,
                    ["x-dead-letter-routing-key"] = _options.Consumer.Queue
                },
                cancellationToken: cancellationToken);

            await channel.QueueBindAsync(
                retry.Queue,
                topology.RetryExchange,
                retry.RoutingKey,
                arguments: null,
                noWait: false,
                cancellationToken: cancellationToken);
        }

        _logger.Information(
            "Configured notification queue {Queue} with organization exchange {OrganizationExchange}, retry, and dead-letter topology",
            _options.Consumer.Queue,
            organizationService.Exchange);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var topologyConfigured = false;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var connection = await _rabbitMqConnection.GetConnectionAsync(stoppingToken);
                await using var channel = await connection.CreateChannelAsync(
                    new CreateChannelOptions(
                        publisherConfirmationsEnabled: true,
                        publisherConfirmationTrackingEnabled: true,
                        outstandingPublisherConfirmationsRateLimiter: null,
                        consumerDispatchConcurrency: null),
                    stoppingToken);

                if (!topologyConfigured)
                {
                    await ConfigureTopologyAsync(channel, stoppingToken);
                    topologyConfigured = true;
                }

                await channel.BasicQosAsync(
                    prefetchSize: 0,
                    prefetchCount: _options.Consumer.PrefetchCount,
                    global: false,
                    cancellationToken: stoppingToken);

                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.ReceivedAsync += (_, delivery) =>
                    HandleDeliveryAsync(channel, delivery, stoppingToken);

                await channel.BasicConsumeAsync(
                    _options.Consumer.Queue,
                    autoAck: false,
                    consumer,
                    stoppingToken);

                await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.Warning(
                    exception,
                    "Notification consumer cannot connect to queue {Queue}; retrying in {DelaySeconds} seconds",
                    _options.Consumer.Queue,
                    _options.Consumer.ReconnectDelaySeconds);

                await Task.Delay(
                    TimeSpan.FromSeconds(_options.Consumer.ReconnectDelaySeconds),
                    stoppingToken);
            }
        }
    }

    private async Task HandleDeliveryAsync(
        IChannel channel,
        BasicDeliverEventArgs delivery,
        CancellationToken stoppingToken)
    {
        try
        {
            var envelope = JsonSerializer.Deserialize<CloudEventEnvelope<CreateNotificationRequestModel>>(
                delivery.Body.Span,
                JsonUtils.SerializerOptions);

            Validate(envelope);

            var bellNotifications = envelope!.Data.Notification is null
                ? []
                : await _notificationPersistence.PersistAsync(envelope, stoppingToken);

            await _notificationBellSender.SendAsync(bellNotifications, stoppingToken);
            await _notificationEmailSender.SendAsync(envelope, bellNotifications, stoppingToken);

            await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, stoppingToken);
        }
        catch (InvalidDataException exception)
        {
            _logger.Warning(exception, "Rejected invalid notification-requested message with delivery tag {DeliveryTag}", delivery.DeliveryTag);
            await channel.BasicRejectAsync(delivery.DeliveryTag, requeue: false, stoppingToken);
        }
        catch (Exception exception)
        {
            await RetryOrDeadLetterAsync(channel, delivery, exception, stoppingToken);
        }
    }

    private async Task RetryOrDeadLetterAsync(
        IChannel channel,
        BasicDeliverEventArgs delivery,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var retryAttempt = GetRetryAttempt(delivery.BasicProperties.Headers);
        if (retryAttempt >= _options.Topology.RetryQueues.Count)
        {
            _logger.Error(
                exception,
                "Notification message with delivery tag {DeliveryTag} reached its retry limit and will be dead-lettered",
                delivery.DeliveryTag);
            await channel.BasicRejectAsync(delivery.DeliveryTag, requeue: false, cancellationToken);
            return;
        }

        var retry = _options.Topology.RetryQueues[retryAttempt];
        var properties = new BasicProperties
        {
            ContentType = delivery.BasicProperties.ContentType,
            DeliveryMode = DeliveryModes.Persistent,
            MessageId = delivery.BasicProperties.MessageId,
            Type = delivery.BasicProperties.Type,
            CorrelationId = delivery.BasicProperties.CorrelationId,
            Headers = new Dictionary<string, object?>
            {
                [_options.Consumer.RetryAttemptHeader] = retryAttempt + 1
            }
        };

        await channel.BasicPublishAsync(
            _options.Topology.RetryExchange,
            retry.RoutingKey,
            mandatory: true,
            properties,
            delivery.Body,
            cancellationToken);

        await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, cancellationToken);

        _logger.Warning(
            exception,
            "Notification message with delivery tag {DeliveryTag} was scheduled for retry {RetryAttempt}",
            delivery.DeliveryTag,
            retryAttempt + 1);
    }

    private void Validate(CloudEventEnvelope<CreateNotificationRequestModel>? envelope)
    {
        var notification = envelope?.Data?.Notification;
        var email = envelope?.Data?.Email;
        if (envelope is null
            || envelope.Id == Guid.Empty
            || string.IsNullOrWhiteSpace(envelope.Source)
            || string.IsNullOrWhiteSpace(envelope.Type)
            || envelope.Data is null
            || string.IsNullOrWhiteSpace(envelope.Data.SourceApp)
            || string.IsNullOrWhiteSpace(envelope.Data.EventType)
            || envelope.Data.CreatedById == Guid.Empty
            || (notification is null && email is null)
            || (notification is not null &&
                (string.IsNullOrWhiteSpace(notification.Title)
                || string.IsNullOrWhiteSpace(notification.Message)
                || notification.Recipients is null
                || notification.Recipients.Count == 0
                || notification.Recipients.Any(recipient =>
                    recipient.UserId == Guid.Empty
                    || string.IsNullOrWhiteSpace(recipient.IdentitySubject))
                || notification.Recipients.Select(recipient => recipient.UserId).Distinct().Count()
                    != notification.Recipients.Count))
            || (email is not null &&
                (string.IsNullOrWhiteSpace(email.TemplateKey)
                || email.Recipients is null
                || email.Recipients.Count == 0
                || email.Recipients.Any(recipient =>
                    recipient.Id == Guid.Empty
                    || string.IsNullOrWhiteSpace(recipient.Email))
                || email.Recipients.Select(recipient => recipient.Id).Distinct().Count()
                    != email.Recipients.Count)))
        {
            throw new UnprocessableEntityException("The message is not a valid notification request CloudEvent.");
        }
    }

    private int GetRetryAttempt(IDictionary<string, object?>? headers)
    {
        if (headers?.TryGetValue(_options.Consumer.RetryAttemptHeader, out var value) != true || value is null)
        {
            return 0;
        }

        return value switch
        {
            byte number => number,
            short number => number,
            int number => number,
            long number when number <= int.MaxValue => (int)number,
            byte[] bytes when int.TryParse(Encoding.UTF8.GetString(bytes), out var number) => number,
            _ => 0
        };
    }

}

public sealed class NotificationPersistence
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly Serilog.ILogger _logger;

    public NotificationPersistence(
        IServiceScopeFactory scopeFactory,
        Serilog.ILogger logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<IReadOnlyList<RealtimeBellNotification>> PersistAsync(
        CloudEventEnvelope<CreateNotificationRequestModel> envelope,
        CancellationToken cancellationToken)
    {
        var payload = envelope.Data.Notification
            ?? throw new UnprocessableEntityException("The notification payload is required for persistence.");

        await using var scope = _scopeFactory.CreateAsyncScope();
        var notificationRepository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
        var notificationRecipientRepository = scope.ServiceProvider.GetRequiredService<INotificationRecipientRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var now = DateTimeOffset.UtcNow;
        var notification = new Notification
        {
            Id = Guid.CreateVersion7(),
            Title = payload.Title,
            Content = payload.Message,
            Data = payload.Data is null
                ? null
                : JsonDocument.Parse(payload.Data.RootElement.GetRawText()),
            CreatedAt = now,
            CreatedById = envelope.Data.CreatedById
        };

        var recipients = payload.Recipients.Select(recipient => new NotificationRecipient
        {
            Id = Guid.CreateVersion7(),
            NotificationId = notification.Id,
            UserId = recipient.UserId,
            UserIdentitySubject = recipient.IdentitySubject,
            CreatedAt = now
        }).ToList();

        notificationRepository.Add(notification);
        notificationRecipientRepository.AddRange(recipients);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.Information(
            "Created notification {NotificationId} from message {MessageId}",
            notification.Id,
            envelope.Id);

        return payload.Recipients.Zip(recipients, (recipient, persistedRecipient) =>
            new RealtimeBellNotification(
                recipient.UserId,
                recipient.IdentitySubject,
                new SendBellNotificationMessage
                {
                    NotificationId = notification.Id,
                    RecipientId = persistedRecipient.Id,
                    EventType = envelope.Data.EventType,
                    Message = notification.Content!,
                    Data = notification.Data is null
                        ? null
                        : JsonDocument.Parse(notification.Data.RootElement.GetRawText()),
                    CreatedAt = notification.CreatedAt
                }))
            .ToList();
    }
}

public sealed class NotificationBellSender
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IRealtimeNotificationPublisher _notificationPublisher;
    private readonly Serilog.ILogger _logger;

    public NotificationBellSender(
        IServiceScopeFactory scopeFactory,
        IRealtimeNotificationPublisher notificationPublisher,
        Serilog.ILogger logger)
    {
        _scopeFactory = scopeFactory;
        _notificationPublisher = notificationPublisher;
        _logger = logger;
    }

    public async Task SendAsync(
        IReadOnlyList<RealtimeBellNotification> notifications,
        CancellationToken cancellationToken)
    {
        foreach (var bellNotification in notifications)
        {
            try
            {
                await _notificationPublisher.PublishBellNotificationAsync(
                    bellNotification.IdentitySubject,
                    bellNotification.Notification,
                    cancellationToken);

                await MarkDeliveryAsync(
                    bellNotification.Notification.RecipientId,
                    bellNotification.IdentitySubject,
                    delivered: true,
                    error: null,
                    cancellationToken);
            }
            catch (Exception exception)
            {
                await MarkDeliveryAsync(
                    bellNotification.Notification.RecipientId,
                    bellNotification.IdentitySubject,
                    delivered: false,
                    error: exception.Message,
                    cancellationToken);

                _logger.Warning(
                    exception,
                    "Bell notification {NotificationId} was stored but could not be pushed to user {IdentitySubject}",
                    bellNotification.Notification.NotificationId,
                    bellNotification.IdentitySubject);
            }
        }
    }

    private async Task MarkDeliveryAsync(
        Guid notificationRecipientId,
        string identitySubject,
        bool delivered,
        string? error,
        CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var deliveryCommandService = scope.ServiceProvider.GetRequiredService<INotificationDeliveryCommandService>();

        await deliveryCommandService.UpsertDeliveryAsync(
            new UpsertNotificationDeliveryRequestModel
            {
                NotificationRecipientId = notificationRecipientId,
                Channel = EnumUtils.ToString(ChannelType.Bell),
                Status = delivered
                    ? EnumUtils.ToString(DeliveryStatus.Delivered)
                    : EnumUtils.ToString(DeliveryStatus.Failed),
                Destination = identitySubject,
                LastError = error
            },
            cancellationToken);
    }
}

public sealed class NotificationEmailSender
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IEmailService _emailService;
    private readonly Serilog.ILogger _logger;

    public NotificationEmailSender(
        IServiceScopeFactory scopeFactory,
        IEmailService emailService,
        Serilog.ILogger logger)
    {
        _scopeFactory = scopeFactory;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task SendAsync(
        CloudEventEnvelope<CreateNotificationRequestModel> envelope,
        IReadOnlyList<RealtimeBellNotification> persistedRecipients,
        CancellationToken cancellationToken)
    {
        var email = envelope.Data.Email;
        if (email is null)
        {
            return;
        }

        await using var scope = _scopeFactory.CreateAsyncScope();
        var emailTemplateRepository = scope.ServiceProvider.GetRequiredService<IEmailTemplateRepository>();

        var template = await emailTemplateRepository.FindFirstByConditionAsync(
            query => query.Where(item =>
                item.IsActive
                && item.TemplateKey == email.TemplateKey),
            cancellationToken: cancellationToken)
            ?? throw new NotFoundException($"Email template with key '{email.TemplateKey}' not found.");

        var subject = BindTemplate(template.SubjectTemplate, email.Data);
        var body = BindTemplate(template.BodyTemplate, email.Data);
        var notificationRecipientIds = persistedRecipients.ToDictionary(
            item => item.UserId,
            item => item.Notification.RecipientId);

        foreach (var recipient in email.Recipients)
        {
            notificationRecipientIds.TryGetValue(recipient.Id, out var notificationRecipientId);

            try
            {
                await _emailService.QueueEmailAsync(
                    new SendEmailToRecipientRequestModel
                    {
                        FromEmail = email.FromEmail,
                        FromName = email.FromName,
                        ToEmail = recipient.Email,
                        Subject = subject,
                        Body = body
                    },
                    cancellationToken);

                if (notificationRecipientId == Guid.Empty)
                {
                    _logger.Warning(
                        "Email for recipient {Recipient} was queued but delivery tracking was skipped because no persisted notification recipient was found",
                        recipient.Email);
                    continue;
                }

                await MarkDeliveryAsync(
                    notificationRecipientId,
                    recipient.Email,
                    delivered: true,
                    error: null,
                    cancellationToken);

                _logger.Information(
                    "Queued email using template {TemplateKey} for recipient {Recipient}",
                    email.TemplateKey,
                    recipient.Email);
            }
            catch (Exception exception)
            {
                if (notificationRecipientId != Guid.Empty)
                {
                    await MarkDeliveryAsync(
                        notificationRecipientId,
                        recipient.Email,
                        delivered: false,
                        error: exception.Message,
                        cancellationToken);
                }

                _logger.Warning(
                    exception,
                    "Email using template {TemplateKey} could not be queued for recipient {Recipient}",
                    email.TemplateKey,
                    recipient.Email);
            }
        }
    }

    private async Task MarkDeliveryAsync(
        Guid notificationRecipientId,
        string email,
        bool delivered,
        string? error,
        CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var deliveryCommandService = scope.ServiceProvider.GetRequiredService<INotificationDeliveryCommandService>();

        await deliveryCommandService.UpsertDeliveryAsync(
            new UpsertNotificationDeliveryRequestModel
            {
                NotificationRecipientId = notificationRecipientId,
                Channel = EnumUtils.ToString(ChannelType.Email),
                Status = delivered
                    ? EnumUtils.ToString(DeliveryStatus.Delivered)
                    : EnumUtils.ToString(DeliveryStatus.Failed),
                Destination = email,
                LastError = error
            },
            cancellationToken);
    }

    private static string BindTemplate(string template, JsonDocument? data)
    {
        try
        {
            var compiledTemplate = Handlebars.Compile(template);
            return compiledTemplate(ToHandlebarsModel(data?.RootElement));
        }
        catch (HandlebarsException exception)
        {
            throw new UnprocessableEntityException(
                "The email template is not a valid Handlebars template.",
                innerException: exception);
        }
    }

    private static IReadOnlyDictionary<string, object?> ToHandlebarsModel(JsonElement? root)
    {
        if (root is not { ValueKind: JsonValueKind.Object } objectRoot)
        {
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        }

        return ToHandlebarsObject(objectRoot);
    }

    private static Dictionary<string, object?> ToHandlebarsObject(JsonElement objectElement)
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (var property in objectElement.EnumerateObject())
        {
            result[property.Name] = ToHandlebarsValue(property.Value);
        }

        return result;
    }

    private static object? ToHandlebarsValue(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.Object => ToHandlebarsObject(value),
            JsonValueKind.Array => value.EnumerateArray()
                .Select(ToHandlebarsValue)
                .ToList(),
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.TryGetInt64(out var integer)
                ? integer
                : value.TryGetDecimal(out var decimalValue)
                    ? decimalValue
                    : value.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            _ => value.GetRawText()
        };
    }
}

public sealed class CloudEventEnvelope<TData>
{
    public Guid Id { get; init; }
    public string Source { get; init; } = null!;
    public string Type { get; init; } = null!;
    public TData Data { get; init; } = default!;
}

public sealed record RealtimeBellNotification(
    Guid UserId,
    string IdentitySubject,
    SendBellNotificationMessage Notification);
