using AccessControl.Domain.Entities;
using AccessControl.Infrastructure.Data;
using AccessControl.Infrastructure.Security;
using AccessControl.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccessControl.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AccessControlDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenService _refreshTokenService;

    public AuthController(
        AccessControlDbContext db,
        UserManager<ApplicationUser> userManager,
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokenService)
    {
        _db = db;
        _userManager = userManager;
        _jwtTokenService = jwtTokenService;
        _refreshTokenService = refreshTokenService;
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "email and password are required" });
        }

        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);
        if (user is null || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            return Unauthorized(new { message = "invalid credentials" });
        }

        var response = await IssueTokensAsync(user, cancellationToken);
        return Ok(response);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return BadRequest(new { message = "refreshToken is required" });
        }

        var refreshTokenHash = _refreshTokenService.ComputeHash(request.RefreshToken);
        var existingToken = await _db.UserRefreshTokens
            .Include(token => token.User)
            .FirstOrDefaultAsync(token => token.TokenHash == refreshTokenHash, cancellationToken);

        if (existingToken is null || existingToken.IsRevoked || existingToken.ExpiresAt <= DateTime.UtcNow || existingToken.User is null)
        {
            return Unauthorized(new { message = "refresh token is invalid or expired" });
        }

        existingToken.RevokedAt = DateTime.UtcNow;
        var response = await IssueTokensAsync(existingToken.User, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(response);
    }

    private async Task<AuthResponse> IssueTokensAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var accessToken = await _jwtTokenService.CreateAccessTokenAsync(user);
        var refreshToken = _refreshTokenService.CreateRefreshToken();
        var roles = await _userManager.GetRolesAsync(user);

        _db.UserRefreshTokens.Add(new UserRefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = refreshToken.TokenHash,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = refreshToken.ExpiresAtUtc
        });

        await _db.SaveChangesAsync(cancellationToken);

        return new AuthResponse
        {
            AccessToken = accessToken.AccessToken,
            AccessTokenExpiresAtUtc = accessToken.ExpiresAtUtc,
            RefreshToken = refreshToken.Token,
            RefreshTokenExpiresAtUtc = refreshToken.ExpiresAtUtc,
            User = new AuthenticatedUserDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName,
                Roles = roles.ToList()
            }
        };
    }

    public sealed class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public sealed class RefreshRequest
    {
        public string RefreshToken { get; set; } = string.Empty;
    }

    public sealed class AuthResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public DateTime AccessTokenExpiresAtUtc { get; set; }
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime RefreshTokenExpiresAtUtc { get; set; }
        public AuthenticatedUserDto User { get; set; } = new();
    }

    public sealed class AuthenticatedUserDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public List<string> Roles { get; set; } = new();
    }
}
