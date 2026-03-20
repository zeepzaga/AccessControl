using System.ComponentModel.DataAnnotations;

namespace AccessControl.Domain.Entities;

public class Schedule
{
    public Guid Id { get; set; }

    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public string ScheduleJson { get; set; } = "{}";

    public DateTime CreatedAt { get; set; }
}
