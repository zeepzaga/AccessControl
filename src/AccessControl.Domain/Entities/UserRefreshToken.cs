namespace AccessControl.Domain.Entities;

public class UserRefreshToken
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }
    public ApplicationUser? User { get; set; }

    [System.ComponentModel.DataAnnotations.MaxLength(128)]
    public string TokenHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }

    public bool IsRevoked => RevokedAt.HasValue;
}
