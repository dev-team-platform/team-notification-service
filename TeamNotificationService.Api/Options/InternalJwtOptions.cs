namespace TeamNotificationService.Api.Options;

public class InternalJwtOptions
{
    public const string SectionName = "InternalJwt";

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string PublicKeyPemPath { get; set; } = string.Empty;
}