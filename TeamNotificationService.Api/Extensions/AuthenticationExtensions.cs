using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using TeamNotificationService.Api.Options;

namespace TeamNotificationService.Api.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddInternalJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<InternalJwtOptions>()
            .BindConfiguration(InternalJwtOptions.SectionName)
            .Validate(options => !string.IsNullOrWhiteSpace(options.Issuer), "InternalJwt Issuer is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.Audience), "InternalJwt Audience is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.PublicKeyPemPath), "InternalJwt PublicKeyPem is required.")
            .ValidateOnStart();

        var internalJwt = configuration
            .GetRequiredSection(InternalJwtOptions.SectionName)
            .Get<InternalJwtOptions>()!;

        var publicKeyPem = File.ReadAllText(internalJwt.PublicKeyPemPath);

        var rsa = RSA.Create();
        rsa.ImportFromPem(publicKeyPem);
        var signingKey = new RsaSecurityKey(rsa);

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = internalJwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = internalJwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey,
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    RequireSignedTokens = true,
                    ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
                    ClockSkew = TimeSpan.FromMinutes(1),
                    NameClaimType = "preferred_username",
                    RoleClaimType = "roles"
                };
            });

        return services;
    }
}
