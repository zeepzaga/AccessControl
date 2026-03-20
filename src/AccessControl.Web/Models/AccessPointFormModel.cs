using AccessControl.Domain.Entities;

namespace AccessControl.Web.Models;

public class AccessPointFormModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Location { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsGuestAccess { get; set; }
    public List<Guid> SelectedEmployeeIds { get; set; } = new();
    public List<Guid> SelectedDepartmentIds { get; set; } = new();
    public List<string> DepartmentNames { get; set; } = new();
    public List<AccessRule> ExistingRules { get; set; } = new();
    public List<SelectableOptionViewModel> AvailableEmployees { get; set; } = new();
    public List<SelectableOptionViewModel> AvailableDepartments { get; set; } = new();

    public static AccessPointFormModel FromAccessPoint(AccessPoint point, IEnumerable<AccessRule>? existingRules = null)
    {
        var rules = existingRules?.ToList() ?? new List<AccessRule>();
        return new AccessPointFormModel
        {
            Id = point.Id,
            Name = point.Name,
            Location = point.Location,
            IsActive = point.IsActive,
            IsGuestAccess = point.IsGuestAccess,
            SelectedEmployeeIds = rules
                .Where(r => r.EmployeeId.HasValue && r.ScheduleId == null && r.ValidFrom == null && r.ValidTo == null)
                .Select(r => r.EmployeeId!.Value)
                .Distinct()
                .ToList(),
            ExistingRules = rules
        };
    }
}
