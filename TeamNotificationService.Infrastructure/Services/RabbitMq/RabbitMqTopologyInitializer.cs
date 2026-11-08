using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using TeamNotificationService.Infrastructure.Options;

namespace TeamNotificationService.Infrastructure.Services.RabbitMq;

public sealed class RabbitMqTopologyInitializer
{
    private readonly RabbitMqConnection _rabbitMqConnection;
    private readonly IOptions<RabbitMqOptions> _rabbitMqOptions;
    private readonly SemaphoreSlim _initializeLock = new(1, 1);
    private volatile bool _initialized;

    public RabbitMqTopologyInitializer(RabbitMqConnection rabbitMqConnection, IOptions<RabbitMqOptions> rabbitMqOptions)
    {
        _rabbitMqConnection = rabbitMqConnection;
        _rabbitMqOptions = rabbitMqOptions;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized)
            return;

        await _initializeLock.WaitAsync(cancellationToken);

        try
        {
            if (_initialized)
                return;

            var connection = await _rabbitMqConnection.GetConnectionAsync(cancellationToken);

            await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

            var declaredExchanges = new Dictionary<string, string>(StringComparer.Ordinal);

            await DeclareSharedExchangesAsync(
                channel,
                declaredExchanges,
                cancellationToken);

            await DeclarePublisherTopologyAsync(
                channel,
                declaredExchanges,
                cancellationToken);

            await DeclareConsumerTopologyAsync(
                channel,
                declaredExchanges,
                cancellationToken);

            _initialized = true;
        }
        finally
        {
            _initializeLock.Release();
        }
    }

    private async Task DeclareSharedExchangesAsync(
        IChannel channel,
        IDictionary<string, string> declaredExchanges,
        CancellationToken cancellationToken)
    {
        await DeclareExchangeAsync(
            channel,
            declaredExchanges,
            _rabbitMqOptions.Value.Retry.Exchange,
            ExchangeType.Direct,
            cancellationToken);

        await DeclareExchangeAsync(
            channel,
            declaredExchanges,
            _rabbitMqOptions.Value.Retry.ReturnExchange,
            ExchangeType.Direct,
            cancellationToken);

        await DeclareExchangeAsync(
            channel,
            declaredExchanges,
            _rabbitMqOptions.Value.Retry.DeadLetterExchange,
            ExchangeType.Direct,
            cancellationToken);
    }

    private async Task DeclarePublisherTopologyAsync(
        IChannel channel,
        IDictionary<string, string> declaredExchanges,
        CancellationToken cancellationToken)
    {
        foreach (var publisher in _rabbitMqOptions.Value.Publishers.Values)
        {
            await DeclareExchangeAsync(
                channel,
                declaredExchanges,
                publisher.Exchange,
                publisher.ExchangeType,
                cancellationToken);
        }
    }

    private async Task DeclareConsumerTopologyAsync(
        IChannel channel,
        IDictionary<string, string> declaredExchanges,
        CancellationToken cancellationToken)
    {
        foreach (var consumer in _rabbitMqOptions.Value.Consumers.Values)
        {
            await DeclareMainQueueAsync(
                channel,
                consumer,
                cancellationToken);

            await DeclareConsumerBindingsAsync(
                channel,
                declaredExchanges,
                consumer,
                cancellationToken);

            await DeclareDeadLetterQueueAsync(
                channel,
                consumer,
                cancellationToken);

            await DeclareRetryQueuesAsync(
                channel,
                consumer,
                cancellationToken);
        }
    }

    private async Task DeclareConsumerBindingsAsync(
        IChannel channel,
        IDictionary<string, string> declaredExchanges,
        RabbitMqConsumerOptions consumer,
        CancellationToken cancellationToken)
    {
        foreach (var binding in consumer.Bindings)
        {
            await DeclareExchangeAsync(
                channel,
                declaredExchanges,
                binding.Exchange,
                binding.ExchangeType,
                cancellationToken);

            foreach (var routingKey in binding.RoutingKeys)
            {
                await channel.QueueBindAsync(
                    queue: consumer.Queue,
                    exchange: binding.Exchange,
                    routingKey: routingKey,
                    arguments: null,
                    noWait: false,
                    cancellationToken: cancellationToken);
            }
        }
    }

    private async Task DeclareMainQueueAsync(
        IChannel channel,
        RabbitMqConsumerOptions consumer,
        CancellationToken cancellationToken)
    {
        await channel.QueueDeclareAsync(
            queue: consumer.Queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: new Dictionary<string, object?>
            {
                ["x-dead-letter-exchange"] = _rabbitMqOptions.Value.Retry.DeadLetterExchange,
                ["x-dead-letter-routing-key"] = consumer.Queue
            },
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            queue: consumer.Queue,
            exchange: _rabbitMqOptions.Value.Retry.ReturnExchange,
            routingKey: consumer.Queue,
            arguments: null,
            noWait: false,
            cancellationToken: cancellationToken);
    }

    private async Task DeclareDeadLetterQueueAsync(
        IChannel channel,
        RabbitMqConsumerOptions consumer,
        CancellationToken cancellationToken)
    {
        var deadLetterQueue =
            GetDeadLetterQueueName(consumer.Queue);

        await channel.QueueDeclareAsync(
            queue: deadLetterQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            queue: deadLetterQueue,
            exchange: _rabbitMqOptions.Value.Retry.DeadLetterExchange,
            routingKey: consumer.Queue,
            arguments: null,
            noWait: false,
            cancellationToken: cancellationToken);
    }

    private async Task DeclareRetryQueuesAsync(
        IChannel channel,
        RabbitMqConsumerOptions consumer,
        CancellationToken cancellationToken)
    {
        foreach (var delay in _rabbitMqOptions.Value.Retry.Delays)
        {
            var retryQueue = GetRetryQueueName(consumer.Queue, delay.Name);

            await channel.QueueDeclareAsync(
                queue: retryQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: new Dictionary<string, object?>
                {
                    ["x-message-ttl"] = delay.MessageTtlMilliseconds,
                    ["x-dead-letter-exchange"] = _rabbitMqOptions.Value.Retry.ReturnExchange,
                    ["x-dead-letter-routing-key"] = consumer.Queue
                },
                cancellationToken: cancellationToken);

            await channel.QueueBindAsync(
                queue: retryQueue,
                exchange: _rabbitMqOptions.Value.Retry.Exchange,
                routingKey: retryQueue,
                arguments: null,
                noWait: false,
                cancellationToken: cancellationToken);
        }
    }

    private static async Task DeclareExchangeAsync(
        IChannel channel,
        IDictionary<string, string> declaredExchanges,
        string exchange,
        string exchangeType,
        CancellationToken cancellationToken)
    {
        if (declaredExchanges.TryGetValue(exchange, out var existingType))
        {
            if (!string.Equals(
                    existingType,
                    exchangeType,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Exchange '{exchange}' is configured with conflicting " +
                    $"types '{existingType}' and '{exchangeType}'.");
            }

            return;
        }

        await channel.ExchangeDeclareAsync(
            exchange: exchange,
            type: exchangeType,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        declaredExchanges.Add(exchange, exchangeType);
    }

    public static string GetDeadLetterQueueName(string queue)
    {
        return $"{queue}.dlq";
    }

    public static string GetRetryQueueName(string queue, string delayName)
    {
        return $"{queue}.retry.{delayName}";
    }
}
