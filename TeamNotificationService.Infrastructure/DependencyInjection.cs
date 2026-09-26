using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using TeamNotificationService.Infrastructure.Options;
using RabbitMQ.Client;
using TeamNotificationService.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using TeamNotificationService.Infrastructure.Persistence;
using TeamNotificationService.Application.Interfaces.Repositories;
using TeamNotificationService.Infrastructure.Repositories;
using TeamNotificationService.Application.Interfaces.Services.Email;
using TeamNotificationService.Infrastructure.Services;
using TeamNotificationService.Application.Interfaces.Contexts;
using TeamNotificationService.Infrastructure.Contexts;

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
                options => !string.IsNullOrWhiteSpace(options.Consumer.Name),
                "RabbitMq Consumer:Name is required.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Consumer.Queue),
                "RabbitMq Consumer:Queue is required.")
            .Validate(
                options => options.Consumer.PrefetchCount > 0,
                "RabbitMq Consumer:PrefetchCount must be greater than zero.")
            .Validate(
                options => options.Consumer.ReconnectDelaySeconds > 0,
                "RabbitMq Consumer:ReconnectDelaySeconds must be greater than zero.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Consumer.RetryAttemptHeader),
                "RabbitMq Consumer:RetryAttemptHeader is required.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Topology.OrganizationService.Exchange)
                    && options.Topology.OrganizationService.RoutingKeys.Count > 0
                    && options.Topology.OrganizationService.RoutingKeys.All(
                        routingKey => !string.IsNullOrWhiteSpace(routingKey)),
                "RabbitMq Topology:OrganizationService must define an exchange and at least one routing key.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Topology.DeadLetterExchange)
                    && !string.IsNullOrWhiteSpace(options.Topology.DeadLetterQueue)
                    && !string.IsNullOrWhiteSpace(options.Topology.RetryExchange)
                    && !string.IsNullOrWhiteSpace(options.Topology.RetryReturnExchange),
                "RabbitMq topology exchanges and dead-letter queue are required.")
            .Validate(
                options => options.Topology.RetryQueues.Count == 3
                    && options.Topology.RetryQueues.All(retry =>
                        !string.IsNullOrWhiteSpace(retry.Queue)
                        && !string.IsNullOrWhiteSpace(retry.RoutingKey)
                        && retry.MessageTtlMilliseconds > 0),
                "RabbitMq Topology:RetryQueues must define the three delayed retry queues.")
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

        services
            .AddOptions<InternalJwtOptions>()
            .BindConfiguration(InternalJwtOptions.SectionName)
            .Validate(options => !string.IsNullOrWhiteSpace(options.Issuer), "InternalJwt Issuer is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Audience), "InternalJwt Audience is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.PublicKeyPemPath), "InternalJwt PublicKeyPem is required.")
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
        services.AddSingleton<NotificationPersistence>();
        services.AddSingleton<NotificationBellSender>();
        services.AddSingleton<NotificationEmailSender>();
        services.AddHostedService<NotificationConsumer>();

        services.AddSingleton<EmailService>();
        services.AddSingleton<IEmailService>(sp => sp.GetRequiredService<EmailService>());
        services.AddHostedService(sp => sp.GetRequiredService<EmailService>());

        // Repositories
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<INotificationRecipientRepository, NotificationRecipientRepository>();
        services.AddScoped<INotificationDeliveryRepository, NotificationDeliveryRepository>();
        services.AddScoped<IEmailTemplateRepository, EmailTemplateRepository>();

        return services;
    }
}
