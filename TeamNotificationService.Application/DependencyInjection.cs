using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using TeamNotificationService.Application.Interfaces.Services.NotificationDeliveries;
using TeamNotificationService.Application.Services.NotificationDeliveries;
using TeamNotificationService.Application.Interfaces.Services.Notifications;
using TeamNotificationService.Application.Services.Notifications;
using TeamNotificationService.Application.Interfaces.Services;
using TeamNotificationService.Application.Services;

namespace TeamNotificationService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<INotificationDeliveryCommandService, NotificationDeliveryCommandService>();
        services.AddScoped<INotificationCommandService, NotificationCommandService>();
        services.AddScoped<INotificationQueryService, NotificationQueryService>();
        services.AddScoped<IHealthCheckService, HealthCheckService>();
        return services;
    }
}