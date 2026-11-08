namespace TeamNotificationService.Infrastructure.Options;

public sealed class SourceAppsOptions
{
    public const string SectionName = "SourceApps";

    public string[] Allowed { get; set; } = [];

    public bool IsAllowed(string? sourceApp) =>
        !string.IsNullOrWhiteSpace(sourceApp)
        && Allowed.Contains(sourceApp, StringComparer.Ordinal);
}
