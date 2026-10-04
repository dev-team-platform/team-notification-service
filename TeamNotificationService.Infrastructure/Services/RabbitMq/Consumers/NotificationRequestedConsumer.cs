using HandlebarsDotNet;
using TeamNotificationService.Application.Interfaces.Services.Notifications;
using TeamNotificationService.Application.Interfaces.Repositories;
using TeamNotificationService.Application.Interfaces.Services.Emails;
using TeamNotificationService.Application.Interfaces.Services.NotificationDeliveries;
using TeamNotificationService.Application.Models.Emails;
using TeamNotificationService.Application.Models.Notifications;
using TeamNotificationService.Domain.Entities;
using TeamNotificationService.Domain.Enums;
using TeamNotificationService.Domain.Exceptions;
using TeamNotificationService.Domain.Utils;
using TeamNotificationService.Application.Interfaces.Services.Messaging;
using System.Text.Json;
using TeamNotificationService.Infrastructure.Services.Email;
using TeamNotificationService.Application.Models.Messaging.Consuming;
using TeamNotificationService.Application.Models.NotificationDeliveries;

namespace TeamNotificationService.Infrastructure.Services.RabbitMq.Consumers;

/// <summary>
/// Processes every CloudEvent whose data uses CreateNotificationRequestModel.
/// Declares its organization-event, retry, and dead-letter topology once at
/// startup, then consumes the configured queue and publishes configured retries.
/// </summary>
public sealed class NotificationRequestedConsumer : IMessagingConsumerHandler
{
    private readonly NotificationPersistence _notificationPersistence;
    private readonly NotificationBellSender _notificationBellSender;
    private readonly NotificationEmailSender _notificationEmailSender;

    public NotificationRequestedConsumer(
        NotificationPersistence notificationPersistence,
        NotificationBellSender notificationBellSender,
        NotificationEmailSender notificationEmailSender)
    {
        _notificationPersistence = notificationPersistence;
        _notificationBellSender = notificationBellSender;
        _notificationEmailSender = notificationEmailSender;
    }

    public async Task HandleAsync(ReadOnlyMemory<byte> body, CancellationToken cancellationToken)
    {
        CloudEventEnvelope<CreateNotificationRequestModel>? envelope;

        try
        {
            envelope = JsonSerializer.Deserialize<CloudEventEnvelope<CreateNotificationRequestModel>>(
                body.Span,
                JsonUtils.SerializerOptions);
        }
        catch (JsonException exception)
        {
            throw new NonRetryableMessageException(
                "The notification request contains invalid JSON.",
                innerException: exception);
        }

        Validate(envelope);

        var bellNotifications =
            envelope!.Data.Notification is null
                ? []
                : await _notificationPersistence.PersistAsync(
                    envelope,
                    cancellationToken);

        await _notificationBellSender.SendAsync(
            bellNotifications,
            cancellationToken);

        try
        {
            await _notificationEmailSender.SendAsync(
                envelope,
                bellNotifications,
                cancellationToken);
        }
        catch (InvalidEmailTemplateException exception)
        {
            throw new NonRetryableMessageException(exception.Message, exception.Details, exception);
        }
    }

    private static void Validate(CloudEventEnvelope<CreateNotificationRequestModel>? envelope)
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
                (
                    string.IsNullOrWhiteSpace(notification.Title)
                    || string.IsNullOrWhiteSpace(notification.Message)
                    || notification.Recipients is null
                    || notification.Recipients.Count == 0
                    || notification.Recipients.Any(recipient =>
                        recipient.UserId == Guid.Empty
                        || string.IsNullOrWhiteSpace(
                            recipient.IdentitySubject))
                    || notification.Recipients
                        .Select(recipient => recipient.UserId)
                        .Distinct()
                        .Count() != notification.Recipients.Count
                ))
            || (email is not null &&
                (
                    string.IsNullOrWhiteSpace(email.TemplateKey)
                    || email.Recipients is null
                    || email.Recipients.Count == 0
                    || email.Recipients.Any(recipient =>
                        recipient.UserId == Guid.Empty
                        || string.IsNullOrWhiteSpace(recipient.Email))
                    || email.Recipients
                        .Select(recipient => recipient.UserId)
                        .Distinct()
                        .Count() != email.Recipients.Count
                )))
        {
            throw new NonRetryableMessageException("The message is not a valid notification request CloudEvent.");
        }
    }
}

public sealed class NotificationPersistence
{
    private readonly INotificationRepository _notificationRepository;
    private readonly INotificationRecipientRepository _notificationRecipientRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly Serilog.ILogger _logger;

    public NotificationPersistence(
        INotificationRepository notificationRepository,
        INotificationRecipientRepository notificationRecipientRepository,
        IUnitOfWork unitOfWork,
        Serilog.ILogger logger)
    {
        _notificationRepository = notificationRepository;
        _notificationRecipientRepository = notificationRecipientRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IReadOnlyList<RealtimeBellNotification>> PersistAsync(
        CloudEventEnvelope<CreateNotificationRequestModel> envelope,
        CancellationToken cancellationToken)
    {
        var payload = envelope.Data.Notification
            ?? throw new NonRetryableMessageException("The notification payload is required for persistence.");

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

        var recipients = payload.Recipients
            .Select(recipient => new NotificationRecipient
            {
                Id = Guid.CreateVersion7(),
                NotificationId = notification.Id,
                UserId = recipient.UserId,
                UserIdentitySubject =
                    recipient.IdentitySubject,
                CreatedAt = now
            })
            .ToList();

        _notificationRepository.Add(notification);
        _notificationRecipientRepository.AddRange(recipients);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.Information(
            "Created notification {NotificationId} from message {MessageId}",
            notification.Id,
            envelope.Id);

        return payload.Recipients
            .Zip(
                recipients,
                (recipient, persistedRecipient) =>
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
    private readonly IRealtimeNotificationPublisher _notificationPublisher;
    private readonly INotificationDeliveryCommandService _deliveryCommandService;
    private readonly Serilog.ILogger _logger;

    public NotificationBellSender(
        IRealtimeNotificationPublisher notificationPublisher,
        INotificationDeliveryCommandService deliveryCommandService,
        Serilog.ILogger logger)
    {
        _notificationPublisher = notificationPublisher;
        _deliveryCommandService = deliveryCommandService;
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

    private Task MarkDeliveryAsync(
        Guid notificationRecipientId,
        string identitySubject,
        bool delivered,
        string? error,
        CancellationToken cancellationToken)
    {
        return _deliveryCommandService.UpsertDeliveryAsync(
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
    private readonly IEmailTemplateRepository _emailTemplateRepository;
    private readonly INotificationDeliveryCommandService _deliveryCommandService;
    private readonly IEmailService _emailService;
    private readonly Serilog.ILogger _logger;

    public NotificationEmailSender(
        IEmailTemplateRepository emailTemplateRepository,
        INotificationDeliveryCommandService deliveryCommandService,
        IEmailService emailService,
        Serilog.ILogger logger)
    {
        _emailTemplateRepository = emailTemplateRepository;
        _deliveryCommandService = deliveryCommandService;
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
            return;

        var template = await _emailTemplateRepository.FindFirstByConditionAsync(
                    query => query.Where(item =>
                        item.IsActive
                        && item.TemplateKey == email.TemplateKey),
                    cancellationToken: cancellationToken)
            ?? throw new NotFoundException($"Email template with key '{email.TemplateKey}' not found.");

        var subject = HandlebarsEmailBinder.BindTemplate(template.SubjectTemplate, email.Data);

        var body = HandlebarsEmailBinder.BindTemplate(template.BodyTemplate, email.Data);

        var notificationRecipientIds = persistedRecipients.ToDictionary(
            item => item.UserId,
            item => item.Notification.RecipientId);

        foreach (var recipient in email.Recipients)
        {
            notificationRecipientIds.TryGetValue(
                recipient.UserId,
                out var notificationRecipientId);

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

    private Task MarkDeliveryAsync(
        Guid notificationRecipientId,
        string email,
        bool delivered,
        string? error,
        CancellationToken cancellationToken)
    {
        return _deliveryCommandService.UpsertDeliveryAsync(
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


}

public sealed record RealtimeBellNotification(
    Guid UserId,
    string IdentitySubject,
    SendBellNotificationMessage Notification);
