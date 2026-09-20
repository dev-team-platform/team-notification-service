namespace TeamNotificationService.Infrastructure.Options;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string Host { get; set; } = null!;
    public int Port { get; set; }
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string DefaultFromEmail { get; set; } = null!;
    public string DefaultFromName { get; set; } = null!;
    public bool UseSsl { get; set; }
    public int QueueCapacity { get; set; }
}
