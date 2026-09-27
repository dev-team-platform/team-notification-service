namespace TeamNotificationService.Api.Options;

public class AuthOptions
{
    public const string SectionName = "Auth";
    public InternalJwtOptions InternalJwt { get; set; } = null!;
}

public class InternalJwtOptions
{
    public string Issuer { get; set; } = null!;

    public string Audience { get; set; } = null!;

    public string PublicKeyPemPath { get; set; } = null!;
}
