using System.Security.Claims;
using System.Text.Encodings.Web;
using AccessControl.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AccessControl.Infrastructure.Security;

public class DeviceTokenAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "DeviceToken";

    private readonly AccessControlDbContext _db;
    private readonly IDeviceTokenService _deviceTokenService;

    public DeviceTokenAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        AccessControlDbContext db,
        IDeviceTokenService deviceTokenService) : base(options, logger, encoder)
    {
        _db = db;
        _deviceTokenService = deviceTokenService;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authorization = Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(authorization) || !authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        var token = authorization["Bearer ".Length..].Trim();
        if (string.IsNullOrWhiteSpace(token))
        {
            return AuthenticateResult.Fail("Device token is missing.");
        }

        var tokenHash = _deviceTokenService.ComputeHash(token);
        var device = await _db.Devices
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.IsActive && d.TokenHash == tokenHash, Context.RequestAborted);

        if (device is null)
        {
            return AuthenticateResult.Fail("Invalid device token.");
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, device.Id.ToString()),
            new(ClaimTypes.Name, device.Name),
            new("token_type", "device"),
            new("device_id", device.Id.ToString())
        };

        if (device.AccessPointId.HasValue)
        {
            claims.Add(new Claim("access_point_id", device.AccessPointId.Value.ToString()));
        }

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return AuthenticateResult.Success(ticket);
    }
}
