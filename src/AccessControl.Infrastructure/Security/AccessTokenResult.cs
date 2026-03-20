namespace AccessControl.Infrastructure.Security;

public sealed record AccessTokenResult(string AccessToken, DateTime ExpiresAtUtc);
