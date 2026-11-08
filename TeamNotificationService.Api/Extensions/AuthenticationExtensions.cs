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
        var authOptions = configuration
            .GetRequiredSection(AuthOptions.SectionName)
            .Get<AuthOptions>()!;

        var publicKeyPem = File.ReadAllText(authOptions.InternalJwt.PublicKeyPemPath);

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
                    ValidIssuer = authOptions.InternalJwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = authOptions.InternalJwt.Audience,
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
