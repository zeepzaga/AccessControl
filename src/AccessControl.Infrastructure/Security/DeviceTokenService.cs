using System.Security.Cryptography;
using System.Text;

namespace AccessControl.Infrastructure.Security;

public class DeviceTokenService : IDeviceTokenService
{
    public DeviceTokenResult CreateToken()
    {
        var token = $"acdv_{Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant()}";
        var rotatedAtUtc = DateTime.UtcNow;
        var hint = token.Length <= 8 ? token : $"...{token[^8..]}";
        return new DeviceTokenResult(token, hint, ComputeHash(token), rotatedAtUtc);
    }

    public string ComputeHash(string token)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hashBytes);
    }
}
