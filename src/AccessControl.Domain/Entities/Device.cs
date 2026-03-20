namespace AccessControl.Domain.Entities;

public class Device
{
    public Guid Id { get; set; }

    [System.ComponentModel.DataAnnotations.MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.MaxLength(200)]
    public string? Location { get; set; }

    public Guid? AccessPointId { get; set; }
    public AccessPoint? AccessPoint { get; set; }

    [System.ComponentModel.DataAnnotations.MaxLength(200)]
    public string? TokenHint { get; set; }

    [System.ComponentModel.DataAnnotations.MaxLength(128)]
    public string? TokenHash { get; set; }

    public DateTime? TokenLastRotatedAt { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}
