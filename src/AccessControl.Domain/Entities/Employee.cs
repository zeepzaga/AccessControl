using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace AccessControl.Domain.Entities;

public class Employee
{
    public Guid Id { get; set; }

    [MaxLength(300)]
    public string FullName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public byte[]? FaceImage { get; set; }
    public float[]? FaceEmbedding { get; set; }
    public DateTime? BiometricUpdatedAt { get; set; }

    public ICollection<NfcCard> Cards { get; set; } = new List<NfcCard>();

    [JsonIgnore]
    public ICollection<EmployeeDepartment> EmployeeDepartments { get; set; } = new List<EmployeeDepartment>();

    [NotMapped]
    public List<string> DepartmentNames { get; set; } = new();
}
