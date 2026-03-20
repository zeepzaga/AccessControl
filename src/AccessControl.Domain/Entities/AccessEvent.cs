using AccessControl.Domain.Enums;

namespace AccessControl.Domain.Entities;

public class AccessEvent
{
    public Guid Id { get; set; }

    public Guid? DeviceId { get; set; }
    public Device? Device { get; set; }

    public Guid? AccessPointId { get; set; }
    public AccessPoint? AccessPoint { get; set; }

    public string? CardUid { get; set; }

    public Guid? EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public DateTime EventTime { get; set; }

    public bool AccessGranted { get; set; }

    public AccessEventReason Reason { get; set; }
}
