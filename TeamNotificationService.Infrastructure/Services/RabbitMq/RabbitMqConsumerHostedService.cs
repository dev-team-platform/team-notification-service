using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog;
using TeamNotificationService.Application.Interfaces.Services.Messaging;
using TeamNotificationService.Domain.Exceptions;
using TeamNotificationService.Infrastructure.Options;

namespace TeamNotificationService.Infrastructure.Services.RabbitMq;

public sealed class RabbitMqConsumerHostedService : BackgroundService
{
    private readonly RabbitMqConnection _rabbitMqConnection;
    private readonly RabbitMqTopologyInitializer _topologyInitializer;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<RabbitMqOptions> _rabbitMqOptions;
    private readonly ILogger _logger;

    public RabbitMqConsumerHostedService(
        RabbitMqConnection rabbitMqConnection,
        RabbitMqTopologyInitializer topologyInitializer,
        IServiceScopeFactory scopeFactory,
        IOptions<RabbitMqOptions> rabbitMqOptions,
        ILogger logger)
    {
        _rabbitMqConnection = rabbitMqConnection;
        _topologyInitializer = topologyInitializer;
        _scopeFactory = scopeFactory;
        _rabbitMqOptions = rabbitMqOptions;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _topologyInitializer.InitializeAsync(stoppingToken);

        var tasks = _rabbitMqOptions.Value.Consumers.Select(
            consumer => RunConsumerAsync(
                consumer.Key,
                consumer.Value,
                stoppingToken));

        await Task.WhenAll(tasks);
    }

    private async Task RunConsumerAsync(
        string consumerName,
        RabbitMqConsumerOptions consumerOptions,
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var connection =
                    await _rabbitMqConnection.GetConnectionAsync(
                        stoppingToken);

                await using var channel =
                    await connection.CreateChannelAsync(
                        new CreateChannelOptions(
                            publisherConfirmationsEnabled: true,
                            publisherConfirmationTrackingEnabled: true),
                        stoppingToken);

                await channel.BasicQosAsync(
                    prefetchSize: 0,
                    prefetchCount: consumerOptions.PrefetchCount,
                    global: false,
                    cancellationToken: stoppingToken);

                var consumer = new AsyncEventingBasicConsumer(channel);

                consumer.ReceivedAsync += (_, delivery) =>
                    HandleDeliveryAsync(
                        consumerName,
                        consumerOptions,
                        channel,
                        delivery,
                        stoppingToken);

                await channel.BasicConsumeAsync(
                    queue: consumerOptions.Queue,
                    autoAck: false,
                    consumer: consumer,
                    cancellationToken: stoppingToken);

                _logger.Information(
                    "RabbitMQ consumer {ConsumerName} started on queue {Queue}",
                    consumerName,
                    consumerOptions.Queue);

                await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                _logger.Warning(
                    exception,
                    "RabbitMQ consumer {ConsumerName} disconnected. Retrying in {DelaySeconds} seconds",
                    consumerName,
                    consumerOptions.ReconnectDelaySeconds);

                await Task.Delay(
                    TimeSpan.FromSeconds(
                        consumerOptions.ReconnectDelaySeconds),
                    stoppingToken);
            }
        }
    }

    private async Task HandleDeliveryAsync(
        string consumerName,
        RabbitMqConsumerOptions consumerOptions,
        IChannel channel,
        BasicDeliverEventArgs delivery,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredKeyedService<IMessagingConsumerHandler>(consumerName);

            await handler.HandleAsync(
                delivery.Body,
                cancellationToken);

            await channel.BasicAckAsync(
                delivery.DeliveryTag,
                multiple: false,
                cancellationToken);
        }
        catch (NonRetryableMessageException exception)
        {
            _logger.Warning(
                exception,
                "Rejecting non-retryable RabbitMQ message. Consumer={ConsumerName}, Queue={Queue}, DeliveryTag={DeliveryTag}",
                consumerName,
                consumerOptions.Queue,
                delivery.DeliveryTag);

            await channel.BasicRejectAsync(
                delivery.DeliveryTag,
                requeue: false,
                cancellationToken);
        }
        catch (Exception exception)
        {
            await RetryOrDeadLetterAsync(
                consumerName,
                consumerOptions,
                channel,
                delivery,
                exception,
                cancellationToken);
        }
    }

    private async Task RetryOrDeadLetterAsync(
        string consumerName,
        RabbitMqConsumerOptions consumerOptions,
        IChannel channel,
        BasicDeliverEventArgs delivery,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var retryAttempt = GetRetryAttempt(delivery.BasicProperties.Headers);

        if (retryAttempt >= _rabbitMqOptions.Value.Retry.Delays.Count)
        {
            _logger.Error(
                exception,
                "RabbitMQ message exceeded retry limit and will be dead-lettered. Consumer={ConsumerName}, Queue={Queue}, DeliveryTag={DeliveryTag}",
                consumerName,
                consumerOptions.Queue,
                delivery.DeliveryTag);

            await channel.BasicRejectAsync(
                delivery.DeliveryTag,
                requeue: false,
                cancellationToken);

            return;
        }

        var delay = _rabbitMqOptions.Value.Retry.Delays[retryAttempt];
        var retryQueue = RabbitMqTopologyInitializer.GetRetryQueueName(consumerOptions.Queue, delay.Name);
        var properties = CreateRetryProperties(delivery, retryAttempt + 1);

        await channel.BasicPublishAsync(
            exchange: _rabbitMqOptions.Value.Retry.Exchange,
            routingKey: retryQueue,
            mandatory: true,
            basicProperties: properties,
            body: delivery.Body,
            cancellationToken: cancellationToken);

        await channel.BasicAckAsync(
            delivery.DeliveryTag,
            multiple: false,
            cancellationToken);

        _logger.Warning(
            exception,
            "RabbitMQ message scheduled for retry {RetryAttempt}. Consumer={ConsumerName}, Queue={Queue}, RetryQueue={RetryQueue}",
            retryAttempt + 1,
            consumerName,
            consumerOptions.Queue,
            retryQueue);
    }

    private BasicProperties CreateRetryProperties(
        BasicDeliverEventArgs delivery,
        int retryAttempt)
    {
        var headers = delivery.BasicProperties.Headers is null
            ? new Dictionary<string, object?>()
            : new Dictionary<string, object?>(delivery.BasicProperties.Headers);

        headers[_rabbitMqOptions.Value.Retry.AttemptHeader] = retryAttempt;

        return new BasicProperties
        {
            ContentType = delivery.BasicProperties.ContentType,
            ContentEncoding = delivery.BasicProperties.ContentEncoding,
            DeliveryMode = DeliveryModes.Persistent,
            MessageId = delivery.BasicProperties.MessageId,
            CorrelationId = delivery.BasicProperties.CorrelationId,
            Type = delivery.BasicProperties.Type,
            AppId = delivery.BasicProperties.AppId,
            Headers = headers
        };
    }

    private int GetRetryAttempt(IDictionary<string, object?>? headers)
    {
        if (headers?.TryGetValue(_rabbitMqOptions.Value.Retry.AttemptHeader, out var value) != true ||
            value is null)
        {
            return 0;
        }

        return value switch
        {
            byte number => number,
            short number => number,
            int number => number,
            long number when number <= int.MaxValue => (int)number,

            byte[] bytes
                when int.TryParse(
                    System.Text.Encoding.UTF8.GetString(bytes),
                    out var number) =>
                number,

            _ => 0
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