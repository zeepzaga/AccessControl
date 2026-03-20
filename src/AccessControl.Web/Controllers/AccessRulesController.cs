using AccessControl.Domain.Entities;
using AccessControl.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AccessControl.Web.Controllers;

public class AccessRulesController : Controller
{
    private readonly ApiClient _api;

    public AccessRulesController(ApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index(
        [FromQuery] Guid? employeeId = null,
        [FromQuery] Guid? accessPointId = null,
        [FromQuery] Guid? scheduleId = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string sort = "employee",
        [FromQuery] bool desc = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var rules = await _api.GetAccessRulesAsync(employeeId, accessPointId, scheduleId, isActive);
        var employees = await _api.GetEmployeesAsync();
        var points = await _api.GetAccessPointsAsync();
        var schedules = await _api.GetSchedulesAsync();
        var model = PagedListViewModel<AccessRule>.Create(Sort(rules, sort, desc), page, pageSize, sort, desc);

        ViewBag.EmployeeId = employeeId;
        ViewBag.AccessPointId = accessPointId;
        ViewBag.ScheduleId = scheduleId;
        ViewBag.IsActive = isActive;
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
        ViewBag.ScheduleOptions = schedules
            .OrderBy(item => item.Name)
            .Select(item => new SearchableSelectOptionViewModel
            {
                Value = item.Id.ToString(),
                Label = item.Name,
                SearchText = item.Name,
                Selected = scheduleId == item.Id
            })
            .ToList();
        ViewBag.Sort = model.Sort;
        ViewBag.Desc = model.Desc;
        ViewBag.PageSize = model.PageSize;
        return View(model);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var rule = await _api.GetAccessRuleAsync(id);
        if (rule is null)
        {
            return NotFound();
        }

        return View(rule);
    }

    public async Task<IActionResult> Create(Guid? employeeId = null, Guid? accessPointId = null, Guid? scheduleId = null)
    {
        var model = new AccessRule
        {
            EmployeeId = employeeId,
            AccessPointId = accessPointId,
            ScheduleId = scheduleId
        };
        await PopulateLists(model.EmployeeId, model.AccessPointId, model.ScheduleId);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AccessRule rule)
    {
        if (!ModelState.IsValid)
        {
            await PopulateLists(rule.EmployeeId, rule.AccessPointId, rule.ScheduleId);
            return View(rule);
        }

        await _api.CreateAccessRuleAsync(rule);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var rule = await _api.GetAccessRuleAsync(id);
        if (rule is null)
        {
            return NotFound();
        }

        await PopulateLists(rule.EmployeeId, rule.AccessPointId, rule.ScheduleId);
        return View(rule);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, AccessRule rule)
    {
        if (id != rule.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            await PopulateLists(rule.EmployeeId, rule.AccessPointId, rule.ScheduleId);
            return View(rule);
        }

        await _api.UpdateAccessRuleAsync(rule);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(Guid id)
    {
        var rule = await _api.GetAccessRuleAsync(id);
        if (rule is null)
        {
            return NotFound();
        }

        return View(rule);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        await _api.DeleteAccessRuleAsync(id);
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateLists(Guid? selectedEmployeeId = null, Guid? selectedAccessPointId = null, Guid? selectedScheduleId = null)
    {
        var employees = await _api.GetEmployeesAsync();
        var points = await _api.GetAccessPointsAsync();
        var schedules = await _api.GetSchedulesAsync();

        ViewBag.EmployeeId = new SelectList(employees, "Id", "FullName", selectedEmployeeId);
        ViewBag.AccessPointId = new SelectList(points, "Id", "Name", selectedAccessPointId);
        ViewBag.ScheduleId = new SelectList(schedules, "Id", "Name", selectedScheduleId);
    }

    private static IEnumerable<AccessRule> Sort(IEnumerable<AccessRule> rules, string sort, bool desc)
    {
        Func<AccessRule, object?> keySelector = sort.ToLowerInvariant() switch
        {
            "accesspoint" => rule => rule.AccessPoint?.Name,
            "schedule" => rule => rule.Schedule?.Name,
            "status" => rule => rule.IsActive,
            _ => rule => rule.Employee?.FullName
        };

        return desc
            ? rules.OrderByDescending(keySelector).ThenBy(rule => rule.Employee?.FullName)
            : rules.OrderBy(keySelector).ThenBy(rule => rule.Employee?.FullName);
    }
}
