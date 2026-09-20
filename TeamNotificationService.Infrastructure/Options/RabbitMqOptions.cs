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
    public string OrganizationEventExchange { get; init; } = "team.organization.events";
    public string OrganizationUserCreatedRoutingKey { get; init; } = "user.created.v1";
    public string DeadLetterExchange { get; init; } = "team.notification.dlx";
    public string DeadLetterQueue { get; init; } = "team.notification.business-events.q.dlq";
    public string RetryExchange { get; init; } = "team.notification.retry";
    public string RetryReturnExchange { get; init; } = "team.notification.retry.return";
    public IReadOnlyList<RabbitMqRetryQueueOptions> RetryQueues { get; init; } = [];
}

public class RabbitMqRetryQueueOptions
{
    public string Queue { get; init; } = null!;
    public string RoutingKey { get; init; } = null!;
    public int MessageTtlMilliseconds { get; init; }
}
