using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using TeamNotificationService.Infrastructure.Options;
using RabbitMQ.Client;
using TeamNotificationService.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using TeamNotificationService.Infrastructure.Persistence;
using TeamNotificationService.Application.Interfaces.Repositories;
using TeamNotificationService.Infrastructure.Repositories;

namespace TeamNotificationService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAppOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<RabbitMqOptions>()
            .BindConfiguration(RabbitMqOptions.SectionName)
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Host),
                "RabbitMq Host is required.")
            .Validate(
                options => options.Port is > 0 and <= 65535,
                "RabbitMq Port must be between 1 and 65535.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Username),
                "RabbitMq Username is required.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Password),
                "RabbitMq Password is required.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.VirtualHost),
                "RabbitMq VirtualHost is required.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Exchange),
                "RabbitMq Exchange is required.")
            .ValidateOnStart();

        return services;
    }

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Database connection string is not configured.");

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());

        // RabbitMQ
        var options = configuration
            .GetRequiredSection(RabbitMqOptions.SectionName)
            .Get<RabbitMqOptions>()
            ?? throw new InvalidOperationException("RabbitMQ configuration is invalid.");

        services.AddSingleton<IConnectionFactory>(_ => new ConnectionFactory
        {
            HostName = options.Host,
            Port = options.Port,
            UserName = options.Username,
            Password = options.Password,
            VirtualHost = options.VirtualHost,
            AutomaticRecoveryEnabled = true,
            TopologyRecoveryEnabled = true
        });

        services.AddSingleton<RabbitMqConnection>();

        // Repositories
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationRecipientRepository, NotificationRecipientRepository>();
        services.AddScoped<INotificationDeliveryRepository, NotificationDeliveryRepository>();
        services.AddScoped<IEmailTemplateRepository, EmailTemplateRepository>();

        return services;
    }
}
