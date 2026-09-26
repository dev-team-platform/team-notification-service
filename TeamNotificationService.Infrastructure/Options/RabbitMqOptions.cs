namespace TeamNotificationService.Infrastructure.Options;

public class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string Host { get; init; } = null!;
    public int Port { get; init; }
    public string Username { get; init; } = null!;
    public string Password { get; init; } = null!;
    public string VirtualHost { get; init; } = null!;
    public RabbitMqConsumerOptions Consumer { get; init; } = new();
    public RabbitMqTopologyOptions Topology { get; init; } = new();
}

public class RabbitMqConsumerOptions
{
    public string Name { get; init; } = "team-notification.requested.v1";
    public string Queue { get; init; } = "team.notification.business-events.q";
    public ushort PrefetchCount { get; init; } = 10;
    public int ReconnectDelaySeconds { get; init; } = 5;
    public string RetryAttemptHeader { get; init; } = "x-retry-attempt";
}

public class RabbitMqTopologyOptions
{
    public PublisherOptions OrganizationService { get; init; } = new();
    public string DeadLetterExchange { get; init; } = string.Empty;
    public string DeadLetterQueue { get; init; } = string.Empty;
    public string RetryExchange { get; init; } = string.Empty;
    public string RetryReturnExchange { get; init; } = string.Empty;
    public IReadOnlyList<RabbitMqRetryQueueOptions> RetryQueues { get; init; } = [];
}

public class RabbitMqRetryQueueOptions
{
    public string Queue { get; init; } = null!;
    public string RoutingKey { get; init; } = null!;
    public int MessageTtlMilliseconds { get; init; }
}

public class PublisherOptions
{
    public string Exchange { get; init; } = string.Empty;
    public List<string> RoutingKeys { get; init; } = [];
}
