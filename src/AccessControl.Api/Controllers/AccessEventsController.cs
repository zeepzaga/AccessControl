using AccessControl.Application.Access;
using AccessControl.Domain.Entities;
using AccessControl.Domain.Enums;
using AccessControl.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccessControl.Api.Controllers;

[ApiController]
[Route("api/access-events")]
public class AccessEventsController : ControllerBase
{
    private static readonly string[] DefaultColumns =
    [
        "event_time",
        "card_uid",
        "employee_name",
        "access_point",
        "device",
        "access_granted",
        "reason"
    ];

    private readonly AccessControlDbContext _db;
    private readonly IAccessEventExporter _exporter;

    public AccessEventsController(AccessControlDbContext db, IAccessEventExporter exporter)
    {
        _db = db;
        _exporter = exporter;
    }

    [HttpGet]
    public async Task<ActionResult<List<AccessEvent>>> GetRecent(
        [FromQuery] int take = 200,
        [FromQuery] string? cardUid = null,
        [FromQuery] Guid? employeeId = null,
        [FromQuery] Guid? accessPointId = null,
        [FromQuery] bool? granted = null,
        [FromQuery] string? reason = null,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null)
    {
        var query = _db.AccessEvents
            .Include(e => e.Employee)
            .Include(e => e.AccessPoint)
            .Include(e => e.Device)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(cardUid))
        {
            query = query.Where(e => e.CardUid != null && EF.Functions.ILike(e.CardUid, $"%{cardUid}%"));
        }

        if (employeeId.HasValue)
        {
            query = query.Where(e => e.EmployeeId == employeeId.Value);
        }

        if (accessPointId.HasValue)
        {
            query = query.Where(e => e.AccessPointId == accessPointId.Value);
        }

        if (granted.HasValue)
        {
            query = query.Where(e => e.AccessGranted == granted.Value);
        }

        if (!string.IsNullOrWhiteSpace(reason))
        {
            if (!Enum.TryParse<AccessEventReason>(reason, ignoreCase: true, out var parsedReason))
            {
                return BadRequest($"Unknown access event reason '{reason}'.");
            }

            query = query.Where(e => e.Reason == parsedReason);
        }

        if (fromUtc.HasValue)
        {
            query = query.Where(e => e.EventTime >= fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            query = query.Where(e => e.EventTime <= toUtc.Value);
        }

        var data = await query
            .OrderByDescending(e => e.EventTime)
            .Take(take)
            .ToListAsync();

        return Ok(data);
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] string format = "csv", [FromQuery] string[]? columns = null, [FromQuery] DateTime? fromUtc = null, [FromQuery] DateTime? toUtc = null)
    {
        var columnList = (columns is { Length: > 0 } ? columns : DefaultColumns).ToList();
        var options = new AccessEventExportOptions(fromUtc, toUtc, columnList);

        if (string.Equals(format, "xlsx", StringComparison.OrdinalIgnoreCase))
        {
            var bytes = await _exporter.ExportXlsxAsync(options);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "access_events.xlsx");
        }

        var csv = await _exporter.ExportCsvAsync(options);
        return File(csv, "text/csv", "access_events.csv");
    }
}
