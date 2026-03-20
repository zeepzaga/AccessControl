namespace AccessControl.Infrastructure.Security;

public sealed record RefreshTokenResult(string Token, DateTime ExpiresAtUtc, string TokenHash);
