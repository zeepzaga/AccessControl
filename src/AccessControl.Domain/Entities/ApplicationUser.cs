using Microsoft.AspNetCore.Identity;

namespace AccessControl.Domain.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    [System.ComponentModel.DataAnnotations.MaxLength(200)]
    public string? FullName { get; set; }

    public DateTime CreatedAt { get; set; }

    public ICollection<UserRefreshToken> RefreshTokens { get; set; } = new List<UserRefreshToken>();
}
