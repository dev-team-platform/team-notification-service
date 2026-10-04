using TeamNotificationService.Api.Hubs;
using TeamNotificationService.Application.Interfaces.Services.Notifications;

namespace TeamNotificationService.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSignalR();
        services.AddSingleton<IRealtimeNotificationPublisher, SignalRRealtimeNotificationPublisher>();
        return services;
    }
}
