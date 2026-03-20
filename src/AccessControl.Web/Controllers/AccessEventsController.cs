using AccessControl.Domain.Entities;
using AccessControl.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace AccessControl.Web.Controllers;

public class AccessEventsController : Controller
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

    private readonly ApiClient _api;

    public AccessEventsController(ApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index(
        [FromQuery] string? cardUid = null,
        [FromQuery] Guid? employeeId = null,
        [FromQuery] Guid? accessPointId = null,
        [FromQuery] bool? granted = null,
        [FromQuery] string? reason = null,
        [FromQuery] DateTime? fromUtc = null,
        [FromQuery] DateTime? toUtc = null,
        [FromQuery] string sort = "eventtime",
        [FromQuery] bool desc = true,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var events = await _api.GetAccessEventsAsync(200, cardUid, employeeId, accessPointId, granted, reason, fromUtc, toUtc);
        var cards = await _api.GetCardsAsync();
        var employees = await _api.GetEmployeesAsync();
        var points = await _api.GetAccessPointsAsync();
        var model = PagedListViewModel<AccessEvent>.Create(Sort(events, sort, desc), page, pageSize, sort, desc);

        ViewBag.CardUid = cardUid;
        ViewBag.EmployeeId = employeeId;
        ViewBag.AccessPointId = accessPointId;
        ViewBag.Granted = granted;
        ViewBag.Reason = reason;
        ViewBag.FromUtc = fromUtc;
        ViewBag.ToUtc = toUtc;
        ViewBag.CardOptions = cards
            .OrderBy(item => item.Uid)
            .Select(item => new SearchableSelectOptionViewModel
            {
                Value = item.Uid,
                Label = $"{item.Uid}{(string.IsNullOrWhiteSpace(item.Employee?.FullName) ? string.Empty : $" - {item.Employee.FullName}")}",
                SearchText = $"{item.Uid} {item.Employee?.FullName}",
                Selected = string.Equals(cardUid, item.Uid, StringComparison.OrdinalIgnoreCase)
            })
            .ToList();
        ViewBag.EmployeeOptions = employees
            .OrderBy(item => item.FullName)
            .Select(item => new SearchableSelectOptionViewModel
            {
                Value = item.Id.ToString(),
                Label = item.FullName,
                SearchText = item.FullName,
                Selected = employeeId == item.Id
            })
            .ToList();
        ViewBag.AccessPointOptions = points
            .OrderBy(item => item.Name)
            .Select(item => new SearchableSelectOptionViewModel
            {
                Value = item.Id.ToString(),
                Label = item.Name,
                SearchText = $"{item.Name} {item.Location}",
                Selected = accessPointId == item.Id
            })
            .ToList();
        ViewBag.Sort = model.Sort;
        ViewBag.Desc = model.Desc;
        ViewBag.PageSize = model.PageSize;
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Export([FromQuery] string format = "csv", [FromQuery] string[]? columns = null, [FromQuery] DateTime? fromUtc = null, [FromQuery] DateTime? toUtc = null)
    {
        var columnList = (columns is { Length: > 0 } ? columns : DefaultColumns).ToList();
        var result = await _api.ExportAccessEventsAsync(format, columnList, fromUtc, toUtc);
        return File(result.Bytes, result.ContentType, result.FileName);
    }

    private static IEnumerable<AccessEvent> Sort(IEnumerable<AccessEvent> events, string sort, bool desc)
    {
        Func<AccessEvent, object?> keySelector = sort.ToLowerInvariant() switch
        {
            "uid" => item => item.CardUid,
            "employee" => item => item.Employee?.FullName,
            "accesspoint" => item => item.AccessPoint?.Name,
            "device" => item => item.Device?.Name,
            "result" => item => item.AccessGranted,
            "reason" => item => item.Reason,
            _ => item => item.EventTime
        };

        return desc
            ? events.OrderByDescending(keySelector).ThenByDescending(item => item.EventTime)
            : events.OrderBy(keySelector).ThenBy(item => item.EventTime);
    }
}
