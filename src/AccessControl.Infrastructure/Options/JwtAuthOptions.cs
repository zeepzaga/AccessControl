namespace AccessControl.Infrastructure.Options;

public class JwtAuthOptions
{
    public const string SectionName = "Auth:Jwt";

    public string Issuer { get; set; } = "AccessControl.Api";
    public string Audience { get; set; } = "AccessControl.Clients";
    public string SigningKey { get; set; } = "dev-signing-key-change-me-please-1234567890";
    public int AccessTokenLifetimeMinutes { get; set; } = 480;
    public int RefreshTokenLifetimeDays { get; set; } = 30;
}
