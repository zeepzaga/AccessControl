namespace AccessControl.Infrastructure.Security;

public interface IRefreshTokenService
{
    RefreshTokenResult CreateRefreshToken();
    string ComputeHash(string token);
}
