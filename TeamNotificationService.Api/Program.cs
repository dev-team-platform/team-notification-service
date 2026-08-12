using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.OpenApi;
using Serilog;
using TeamNotificationService.Application;
using TeamNotificationService.Infrastructure;

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
builder.Services.AddAuthorization();

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;

    options.ApiVersionReader =
        new UrlSegmentApiVersionReader();
});

builder.Services.AddVersionedApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCors", policy =>
    {
        if (allowedOrigins.Length == 0)
        {
            return;
        }

        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
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

if (isDeployedEnvironment)
{
    app.UseForwardedHeaders();
}

app.UseSerilogRequestLogging();

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

app.UseCors("DefaultCors");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();