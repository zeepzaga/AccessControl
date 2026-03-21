namespace AccessControl.Domain.Entities;

public class AccessRule
{
    public Guid Id { get; set; }

    public Guid? AccessPointId { get; set; }
    public AccessPoint? AccessPoint { get; set; }

    public Guid? ScheduleId { get; set; }
    public Schedule? Schedule { get; set; }

    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }

    public bool IsActive { get; set; } = true;
}
