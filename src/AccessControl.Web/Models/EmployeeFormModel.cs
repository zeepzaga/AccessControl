using AccessControl.Domain.Entities;

namespace AccessControl.Web.Models;

public class EmployeeFormModel
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string DepartmentNamesInput { get; set; } = string.Empty;
    public byte[]? FaceImage { get; set; }
    public float[]? FaceEmbedding { get; set; }
    public DateTime? BiometricUpdatedAt { get; set; }

    public static EmployeeFormModel FromEmployee(Employee employee)
    {
        return new EmployeeFormModel
        {
            Id = employee.Id,
            FullName = employee.FullName,
            IsActive = employee.IsActive,
            DepartmentNamesInput = string.Join(", ", employee.DepartmentNames),
            FaceImage = employee.FaceImage,
            FaceEmbedding = employee.FaceEmbedding,
            BiometricUpdatedAt = employee.BiometricUpdatedAt
        };
    }
}
