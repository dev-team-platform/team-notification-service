using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using TeamNotificationService.Application.Interfaces.Services.NotificationDeliveries;
using TeamNotificationService.Application.Services.NotificationDeliveries;

namespace TeamNotificationService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<INotificationDeliveryCommandService, NotificationDeliveryCommandService>();
        return services;
    }
}