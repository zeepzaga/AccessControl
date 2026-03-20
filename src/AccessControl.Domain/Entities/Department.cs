using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AccessControl.Domain.Entities;

public class Department
{
    public Guid Id { get; set; }

    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [JsonIgnore]
    public ICollection<EmployeeDepartment> EmployeeDepartments { get; set; } = new List<EmployeeDepartment>();

    [JsonIgnore]
    public ICollection<DepartmentAccessPoint> DepartmentAccessPoints { get; set; } = new List<DepartmentAccessPoint>();
}
