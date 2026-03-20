using System.Text.Json.Serialization;

namespace AccessControl.Domain.Entities;

public class DepartmentAccessPoint
{
    public Guid DepartmentId { get; set; }
    public Department? Department { get; set; }

    public Guid AccessPointId { get; set; }

    [JsonIgnore]
    public AccessPoint? AccessPoint { get; set; }
}
