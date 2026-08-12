namespace TeamNotificationService.Infrastructure.Options;

public class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string Host { get; init; } = null!;
    public int Port { get; init; }
    public string Username { get; init; } = null!;
    public string Password { get; init; } = null!;
    public string VirtualHost { get; init; } = null!;
    public string Exchange { get; init; } = null!;
}