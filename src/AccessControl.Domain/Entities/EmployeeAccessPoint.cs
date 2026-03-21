using System.Text.Json.Serialization;

namespace AccessControl.Domain.Entities;

public class EmployeeAccessPoint
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public Guid AccessPointId { get; set; }

    [JsonIgnore]
    public AccessPoint? AccessPoint { get; set; }
}
