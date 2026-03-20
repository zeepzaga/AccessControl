namespace AccessControl.Infrastructure.Security;

public sealed record DeviceTokenResult(string PlainTextToken, string TokenHint, string TokenHash, DateTime RotatedAtUtc);
