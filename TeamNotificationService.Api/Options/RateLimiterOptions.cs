namespace TeamNotificationService.Api.Options;

public sealed class RateLimiterOptions
{
    public const string SectionName = "RateLimiter";

    public DefaultRateLimiterOptions Default { get; init; } = new();
}

public sealed class DefaultRateLimiterOptions
{
    public int TokenLimit { get; init; }
    public int TokensPerPeriod { get; init; }
    public TimeSpan ReplenishmentPeriod { get; init; }
    public int QueueLimit { get; init; }
}
