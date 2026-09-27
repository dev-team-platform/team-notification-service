using TeamNotificationService.Api.Hubs;
using TeamNotificationService.Application.Interfaces.Messaging;

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