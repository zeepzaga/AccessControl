using AccessControl.Domain.Entities;
using AccessControl.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccessControl.Api.Controllers;

[ApiController]
[Route("api/access-points")]
public class AccessPointsController : ControllerBase
{
    private readonly AccessControlDbContext _db;

    public AccessPointsController(AccessControlDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<AccessPoint>>> GetAll([FromQuery] string? q = null, [FromQuery] bool? isActive = null)
    {
        IQueryable<AccessPoint> query = _db.AccessPoints
            .Include(a => a.DepartmentAccessPoints)
            .ThenInclude(link => link.Department)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(a => EF.Functions.ILike(a.Name, $"%{q}%") || (a.Location != null && EF.Functions.ILike(a.Location, $"%{q}%")));
        }

        if (isActive.HasValue)
        {
            query = query.Where(a => a.IsActive == isActive.Value);
        }

        var data = await query.OrderBy(a => a.Name).ToListAsync();
        foreach (var point in data)
        {
            PopulateDepartmentNames(point);
        }

        return Ok(data);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AccessPoint>> GetById(Guid id)
    {
        var point = await _db.AccessPoints
            .Include(a => a.DepartmentAccessPoints)
            .ThenInclude(link => link.Department)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id);

        if (point is null)
        {
            return NotFound();
        }

        PopulateDepartmentNames(point);
        return Ok(point);
    }

    [HttpPost]
    public async Task<ActionResult<AccessPoint>> Create(AccessPointUpsertRequest request)
    {
        var point = new AccessPoint
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Location = string.IsNullOrWhiteSpace(request.Location) ? null : request.Location.Trim(),
            IsActive = request.IsActive,
            IsGuestAccess = request.IsGuestAccess,
            CreatedAt = DateTime.UtcNow
        };

        _db.AccessPoints.Add(point);
        await SyncQuickAccessRulesAsync(point.Id, request.SelectedEmployeeIds, CancellationToken.None);
        await SyncDepartmentsAsync(point, request.SelectedDepartmentIds, CancellationToken.None);
        await _db.SaveChangesAsync();
        PopulateDepartmentNames(point);
        return CreatedAtAction(nameof(GetById), new { id = point.Id }, point);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, AccessPointUpsertRequest request)
    {
        var existing = await _db.AccessPoints
            .Include(a => a.DepartmentAccessPoints)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (existing is null)
        {
            return NotFound();
        }

        existing.Name = request.Name.Trim();
        existing.Location = string.IsNullOrWhiteSpace(request.Location) ? null : request.Location.Trim();
        existing.IsActive = request.IsActive;
        existing.IsGuestAccess = request.IsGuestAccess;

        await SyncQuickAccessRulesAsync(id, request.SelectedEmployeeIds, CancellationToken.None);
        await SyncDepartmentsAsync(existing, request.SelectedDepartmentIds, CancellationToken.None);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var existing = await _db.AccessPoints.FindAsync(id);
        if (existing is null)
        {
            return NotFound();
        }

        _db.AccessPoints.Remove(existing);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private async Task SyncQuickAccessRulesAsync(Guid accessPointId, IEnumerable<Guid>? employeeIds, CancellationToken cancellationToken)
    {
        var normalizedEmployeeIds = employeeIds?
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToHashSet() ?? [];

        var simpleRules = await _db.AccessRules
            .Where(r => r.AccessPointId == accessPointId && r.ScheduleId == null && r.ValidFrom == null && r.ValidTo == null)
            .ToListAsync(cancellationToken);

        foreach (var existingRule in simpleRules.Where(r => !r.EmployeeId.HasValue || !normalizedEmployeeIds.Contains(r.EmployeeId.Value)).ToList())
        {
            _db.AccessRules.Remove(existingRule);
        }

        var existingEmployeeIds = simpleRules
            .Where(r => r.EmployeeId.HasValue)
            .Select(r => r.EmployeeId!.Value)
            .ToHashSet();

        foreach (var employeeId in normalizedEmployeeIds.Where(id => !existingEmployeeIds.Contains(id)))
        {
            _db.AccessRules.Add(new AccessRule
            {
                Id = Guid.NewGuid(),
                EmployeeId = employeeId,
                AccessPointId = accessPointId,
                IsActive = true
            });
        }
    }

    private async Task SyncDepartmentsAsync(AccessPoint point, IEnumerable<Guid>? departmentIds, CancellationToken cancellationToken)
    {
        var normalizedDepartmentIds = departmentIds?
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToHashSet() ?? [];

        var existingLinks = await _db.DepartmentAccessPoints
            .Where(link => link.AccessPointId == point.Id)
            .ToListAsync(cancellationToken);

        foreach (var staleLink in existingLinks.Where(link => !normalizedDepartmentIds.Contains(link.DepartmentId)))
        {
            _db.DepartmentAccessPoints.Remove(staleLink);
        }

        var currentDepartmentIds = existingLinks
            .Select(link => link.DepartmentId)
            .ToHashSet();

        foreach (var departmentId in normalizedDepartmentIds.Where(id => !currentDepartmentIds.Contains(id)))
        {
            _db.DepartmentAccessPoints.Add(new DepartmentAccessPoint
            {
                AccessPointId = point.Id,
                DepartmentId = departmentId
            });
        }
    }

    private static void PopulateDepartmentNames(AccessPoint point)
    {
        point.DepartmentNames = point.DepartmentAccessPoints
            .Where(link => link.Department != null)
            .Select(link => link.Department!.Name)
            .OrderBy(name => name)
            .ToList();
    }

    public sealed class AccessPointUpsertRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Location { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsGuestAccess { get; set; }
        public List<Guid> SelectedEmployeeIds { get; set; } = new();
        public List<Guid> SelectedDepartmentIds { get; set; } = new();
    }
}
