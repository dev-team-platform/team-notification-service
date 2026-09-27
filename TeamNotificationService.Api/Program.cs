using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.OpenApi;
using Serilog;
using TeamNotificationService.Api.Constants;
using TeamNotificationService.Api.Extensions;
using TeamNotificationService.Api.Hubs;
using TeamNotificationService.Application.Interfaces.Messaging;
using TeamNotificationService.Application;
using TeamNotificationService.Infrastructure;
using TeamNotificationService.Api.Middlewares;
using TeamNotificationService.Application.Interfaces.Services;
using TeamNotificationService.Api;

var builder = WebApplication.CreateBuilder(args);

var isDeployedEnvironment =
    builder.Environment.IsEnvironment("Dev")
    || builder.Environment.IsEnvironment("Test")
    || builder.Environment.IsStaging()
    || builder.Environment.IsProduction();

var exposeApiDocs =
    builder.Environment.IsDevelopment()
    || builder.Environment.IsEnvironment("Dev")
    || builder.Environment.IsEnvironment("Test");

builder.Services.AddAppOptions(builder.Configuration);
builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddInternalJwtAuthentication(builder.Configuration);
builder.Services.AddNAppRateLimiter(builder.Configuration);
builder.Services.AddAuthorization();
builder.Services.AddApi(builder.Configuration);


builder.Services.AddControllers();

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
});

builder.Services.AddVersionedApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Team Organization API",
        Version = "v1"
    });
});

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services);
});

if (isDeployedEnvironment)
{
    var trustedNetworks = builder.Configuration
        .GetSection("ReverseProxy:TrustedNetworks")
        .Get<string[]>()
        ?? [];

    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders =
            ForwardedHeaders.XForwardedFor
            | ForwardedHeaders.XForwardedProto
            | ForwardedHeaders.XForwardedHost;

        foreach (var network in trustedNetworks)
        {
            options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
        }
    });
}

var app = builder.Build();

// Warm up the application by performing a health check before accepting requests
using (var scope = app.Services.CreateScope())
{
    var healthCheckService = scope.ServiceProvider.GetRequiredService<IHealthCheckService>();
    await healthCheckService.HealthCheckAsync();
}

if (isDeployedEnvironment)
{
    app.UseForwardedHeaders();
}

app.UseSerilogRequestLogging();
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

if (isDeployedEnvironment)
{
    app.UseHttpsRedirection();
}

if (exposeApiDocs)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications")
    .RequireRateLimiting(RateLimiterPolicies.Default);

app.Run();
