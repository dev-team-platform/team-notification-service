using System.Threading.RateLimiting;
using TeamNotificationService.Api.Constants;
using TeamNotificationService.Api.Options;

namespace TeamNotificationService.Api.Extensions;

public static class RateLimiterExtensions
{
    public static IServiceCollection AddNAppRateLimiter(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<RateLimiterOptions>()
            .BindConfiguration(RateLimiterOptions.SectionName)
            .Validate(x => x.Default.TokenLimit > 0, "RateLimiter:Default:TokenLimit must be positive.")
            .Validate(x => x.Default.TokensPerPeriod > 0, "RateLimiter:Default:TokensPerPeriod must be positive.")
            .Validate(x => x.Default.ReplenishmentPeriod > TimeSpan.Zero, "RateLimiter:Default:ReplenishmentPeriod must be positive.")
            .Validate(x => x.Default.QueueLimit >= 0, "RateLimiter:Default:QueueLimit cannot be negative.")
            .ValidateOnStart();

        var options = configuration
            .GetRequiredSection(RateLimiterOptions.SectionName)
            .Get<RateLimiterOptions>()!;

        services.AddRateLimiter(rateLimiterOptions =>
        {
            rateLimiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            rateLimiterOptions.AddPolicy(RateLimiterPolicies.Default, context =>
                RateLimitPartition.GetTokenBucketLimiter(
                    GetClientIpAddress(context),
                    _ => new TokenBucketRateLimiterOptions
                    {
                        TokenLimit = options.Default.TokenLimit,
                        TokensPerPeriod = options.Default.TokensPerPeriod,
                        ReplenishmentPeriod = options.Default.ReplenishmentPeriod,
                        QueueLimit = options.Default.QueueLimit,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        AutoReplenishment = true
                    }));
        });

        return services;
    }

    private static string GetClientIpAddress(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
