using System.Text.Json.Serialization;

namespace AccessControl.Domain.Entities;

public class EmployeeDepartment
{
    public Guid EmployeeId { get; set; }
    [JsonIgnore]
    public Employee? Employee { get; set; }

    public Guid DepartmentId { get; set; }
    public Department? Department { get; set; }
}
