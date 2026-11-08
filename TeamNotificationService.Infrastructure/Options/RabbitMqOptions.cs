namespace TeamNotificationService.Infrastructure.Options;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";
    public string Host { get; init; } = null!;
    public int Port { get; init; }
    public string Username { get; init; } = null!;
    public string Password { get; init; } = null!;
    public string VirtualHost { get; init; } = null!;

    public RabbitMqRetryOptions Retry { get; init; } = new();
    public Dictionary<string, RabbitMqConsumerOptions> Consumers { get; init; } = [];
    public Dictionary<string, RabbitMqPublisherOptions> Publishers { get; init; } = [];
}

public sealed class RabbitMqConsumerOptions
{
    public string Queue { get; init; } = null!;
    public ushort PrefetchCount { get; init; } = 10;
    public int ReconnectDelaySeconds { get; init; } = 5;
    public IReadOnlyList<RabbitMqBindingOptions> Bindings { get; init; } = [];
}

public sealed class RabbitMqBindingOptions
{
    public string Exchange { get; init; } = null!;
    public string ExchangeType { get; init; } = "topic";
    public IReadOnlyList<string> RoutingKeys { get; init; } = [];
}

public sealed class RabbitMqPublisherOptions
{
    public string Exchange { get; init; } = null!;
    public string ExchangeType { get; init; } = "topic";
}

public sealed class RabbitMqRetryOptions
{
    public string AttemptHeader { get; init; } = "x-retry-attempt";
    public string Exchange { get; init; } = null!;
    public string ReturnExchange { get; init; } = null!;
    public string DeadLetterExchange { get; init; } = null!;
    public IReadOnlyList<RabbitMqRetryDelayOptions> Delays { get; init; } = [];
}

public sealed class RabbitMqRetryDelayOptions
{
    public string Name { get; init; } = null!;
    public int MessageTtlMilliseconds { get; init; }
}
