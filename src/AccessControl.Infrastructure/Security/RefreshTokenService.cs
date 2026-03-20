using System.Security.Cryptography;
using System.Text;
using AccessControl.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace AccessControl.Infrastructure.Security;

public class RefreshTokenService : IRefreshTokenService
{
    private readonly JwtAuthOptions _options;

    public RefreshTokenService(IOptions<JwtAuthOptions> options)
    {
        _options = options.Value;
    }

    public RefreshTokenResult CreateRefreshToken()
    {
        var tokenBytes = RandomNumberGenerator.GetBytes(48);
        var token = Convert.ToBase64String(tokenBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');

        var expiresAtUtc = DateTime.UtcNow.AddDays(_options.RefreshTokenLifetimeDays);
        return new RefreshTokenResult(token, expiresAtUtc, ComputeHash(token));
    }

    public string ComputeHash(string token)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hashBytes);
    }
}
