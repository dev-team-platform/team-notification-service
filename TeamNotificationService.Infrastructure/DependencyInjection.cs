using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using TeamNotificationService.Infrastructure.Options;
using RabbitMQ.Client;
using Microsoft.EntityFrameworkCore;
using TeamNotificationService.Infrastructure.Persistence;
using TeamNotificationService.Application.Interfaces.Repositories;
using TeamNotificationService.Infrastructure.Repositories;
using TeamNotificationService.Application.Interfaces.Services.Emails;
using TeamNotificationService.Application.Interfaces.Contexts;
using TeamNotificationService.Infrastructure.Contexts;
using TeamNotificationService.Infrastructure.Services.RabbitMq;
using TeamNotificationService.Infrastructure.Services.Email;
using TeamNotificationService.Application.Interfaces.Services.Messaging;
using TeamNotificationService.Infrastructure.Services.RabbitMq.Consumers;

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
            .ValidateOnStart();

        services
            .AddOptions<EmailOptions>()
            .BindConfiguration(EmailOptions.SectionName)
            .Validate(options => !string.IsNullOrWhiteSpace(options.Host), "Email Host is required.")
            .Validate(options => options.Port is > 0 and <= 65535, "Email Port must be between 1 and 65535.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Username), "Email Username is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Password), "Email Password is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.DefaultFromEmail), "Email DefaultFromEmail is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.DefaultFromName), "Email DefaultFromName is required.")
            .Validate(options => options.QueueCapacity > 0, "Email QueueCapacity must be greater than zero.")
            .ValidateOnStart();

        return services;
    }

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserContext, CurrentUserContext>();

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
        services.AddSingleton<RabbitMqTopologyInitializer>();
        services.AddHostedService<RabbitMqConsumerHostedService>();
        services.AddSingleton<RabbitMqPublisherHostedService>();
        services.AddHostedService(sp => sp.GetRequiredService<RabbitMqPublisherHostedService>());

        services.AddKeyedScoped<IMessagingConsumerHandler, NotificationRequestedConsumer>("NotificationRequested");
        services.AddScoped<NotificationPersistence>();
        services.AddScoped<NotificationBellSender>();
        services.AddScoped<NotificationEmailSender>();

        // Email
        services.AddSingleton<EmailService>();
        services.AddSingleton<IEmailService>(sp => sp.GetRequiredService<EmailService>());
        services.AddHostedService(sp => sp.GetRequiredService<EmailService>());

        // Repositories
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationRecipientRepository, NotificationRecipientRepository>();
        services.AddScoped<INotificationDeliveryRepository, NotificationDeliveryRepository>();
        services.AddScoped<IEmailTemplateRepository, EmailTemplateRepository>();
        services.AddScoped<IHealthCheckRepository, HealthCheckRepository>();

        return services;
    }
}
