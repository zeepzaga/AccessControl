using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace AccessControl.Web.Security;

public class ApiAuthenticationService
{
    public const string AuthClientName = "AccessControl.AuthApi";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ApiAuthenticationService(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
    {
        _httpClientFactory = httpClientFactory;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<LoginResult> LoginAsync(string email, string password, bool rememberMe, CancellationToken cancellationToken = default)
    {
        using var client = _httpClientFactory.CreateClient(AuthClientName);
        using var response = await client.PostAsJsonAsync("api/auth/login", new LoginRequest
        {
            Email = email,
            Password = password
        }, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            return LoginResult.Failed("Неверный email или пароль.");
        }

        if (!response.IsSuccessStatusCode)
        {
            return LoginResult.Failed("Не удалось выполнить вход через API.");
        }

        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken: cancellationToken);
        if (authResponse is null)
        {
            return LoginResult.Failed("API вернул пустой ответ авторизации.");
        }

        var context = _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("Current HttpContext is unavailable.");
        var principal = CreatePrincipal(authResponse.User);
        var properties = CreateAuthenticationProperties(authResponse, rememberMe);

        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, properties);
        return LoginResult.Succeeded();
    }

    public async Task<string?> GetValidAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context is null)
        {
            return null;
        }

        var authResult = await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (!authResult.Succeeded || authResult.Principal is null || authResult.Properties is null)
        {
            return null;
        }

        var accessToken = authResult.Properties.GetTokenValue("access_token");
        var refreshToken = authResult.Properties.GetTokenValue("refresh_token");
        var accessTokenExpiresAtRaw = authResult.Properties.GetTokenValue("access_token_expires_at");

        if (!string.IsNullOrWhiteSpace(accessToken)
            && DateTimeOffset.TryParse(accessTokenExpiresAtRaw, out var accessTokenExpiresAt)
            && accessTokenExpiresAt > DateTimeOffset.UtcNow.AddMinutes(1))
        {
            return accessToken;
        }

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return null;
        }

        var refreshed = await RefreshAsync(refreshToken, cancellationToken);
        if (refreshed is null)
        {
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return null;
        }

        var properties = authResult.Properties;
        properties.StoreTokens(CreateTokens(refreshed));
        properties.ExpiresUtc = refreshed.RefreshTokenExpiresAtUtc;

        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, authResult.Principal, properties);
        return refreshed.AccessToken;
    }

    public async Task LogoutAsync()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context is null)
        {
            return;
        }

        await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    private async Task<AuthResponse?> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        using var client = _httpClientFactory.CreateClient(AuthClientName);
        using var response = await client.PostAsJsonAsync("api/auth/refresh", new RefreshRequest
        {
            RefreshToken = refreshToken
        }, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken: cancellationToken);
    }

    private static ClaimsPrincipal CreatePrincipal(AuthenticatedUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, string.IsNullOrWhiteSpace(user.FullName) ? user.Email : user.FullName),
            new(ClaimTypes.Email, user.Email)
        };

        if (!string.IsNullOrWhiteSpace(user.FullName))
        {
            claims.Add(new Claim("full_name", user.FullName));
        }

        foreach (var role in user.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    }

    private static AuthenticationProperties CreateAuthenticationProperties(AuthResponse response, bool rememberMe)
    {
        var properties = new AuthenticationProperties
        {
            IsPersistent = rememberMe,
            ExpiresUtc = response.RefreshTokenExpiresAtUtc,
            AllowRefresh = true
        };

        properties.StoreTokens(CreateTokens(response));
        return properties;
    }

    private static IEnumerable<AuthenticationToken> CreateTokens(AuthResponse response)
    {
        return new[]
        {
            new AuthenticationToken { Name = "access_token", Value = response.AccessToken },
            new AuthenticationToken { Name = "refresh_token", Value = response.RefreshToken },
            new AuthenticationToken { Name = "access_token_expires_at", Value = response.AccessTokenExpiresAtUtc.ToString("o") },
            new AuthenticationToken { Name = "refresh_token_expires_at", Value = response.RefreshTokenExpiresAtUtc.ToString("o") }
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
        public DateTimeOffset AccessTokenExpiresAtUtc { get; set; }
        public string RefreshToken { get; set; } = string.Empty;
        public DateTimeOffset RefreshTokenExpiresAtUtc { get; set; }
        public AuthenticatedUser User { get; set; } = new();
    }

    public sealed class AuthenticatedUser
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public List<string> Roles { get; set; } = new();
    }

    public sealed record LoginResult(bool Success, string? ErrorMessage)
    {
        public static LoginResult Succeeded() => new(true, null);
        public static LoginResult Failed(string message) => new(false, message);
    }
}
