using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AccessControl.Domain.Entities;

public class AccessPoint
{
    public Guid Id { get; set; }

    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Location { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsGuestAccess { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<AccessRule> AccessRules { get; set; } = new List<AccessRule>();

    [JsonIgnore]
    public ICollection<DepartmentAccessPoint> DepartmentAccessPoints { get; set; } = new List<DepartmentAccessPoint>();

    [JsonIgnore]
    public ICollection<EmployeeAccessPoint> EmployeeAccessPoints { get; set; } = new List<EmployeeAccessPoint>();

    [NotMapped]
    public List<string> DepartmentNames { get; set; } = new();
}
