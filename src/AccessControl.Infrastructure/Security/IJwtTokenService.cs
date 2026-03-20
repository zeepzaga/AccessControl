using AccessControl.Domain.Entities;

namespace AccessControl.Infrastructure.Security;

public interface IJwtTokenService
{
    Task<AccessTokenResult> CreateAccessTokenAsync(ApplicationUser user);
}
