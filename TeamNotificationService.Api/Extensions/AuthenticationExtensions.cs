using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using TeamNotificationService.Infrastructure.Options;

namespace TeamNotificationService.Api.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddInternalJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var internalJwt = configuration
            .GetRequiredSection(InternalJwtOptions.SectionName)
            .Get<InternalJwtOptions>()
            ?? throw new InvalidOperationException("InternalJwt configuration is required.");

        var rsa = RSA.Create();
        rsa.ImportFromPem(internalJwt.PublicKeyPem);
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
