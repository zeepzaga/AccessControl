using AccessControl.Domain.Entities;
using AccessControl.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AccessControl.Web.Controllers;

public class AccessRulesController : AppController
{
    private readonly ApiClient _api;

    public AccessRulesController(ApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index(
        [FromQuery] Guid? accessPointId = null,
        [FromQuery] Guid? scheduleId = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string sort = "accesspoint",
        [FromQuery] bool desc = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var rules = await _api.GetAccessRulesAsync(accessPointId, scheduleId, isActive);
        var points = await _api.GetAccessPointsAsync();
        var schedules = await _api.GetSchedulesAsync();
        var model = PagedListViewModel<AccessRule>.Create(Sort(rules, sort, desc), page, pageSize, sort, desc);

        ViewBag.AccessPointId = accessPointId;
        ViewBag.ScheduleId = scheduleId;
        ViewBag.IsActive = isActive;
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

    public async Task<IActionResult> Create(Guid? accessPointId = null, Guid? scheduleId = null)
    {
        var model = new AccessRule
        {
            AccessPointId = accessPointId,
            ScheduleId = scheduleId
        };
        await PopulateLists(model.AccessPointId, model.ScheduleId);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AccessRule rule)
    {
        if (!ModelState.IsValid)
        {
            await PopulateLists(rule.AccessPointId, rule.ScheduleId);
            return View(rule);
        }

        try
        {
            await _api.CreateAccessRuleAsync(rule);
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            await PopulateLists(rule.AccessPointId, rule.ScheduleId);
            SetScreenError("Не удалось сохранить правило доступа.", ex);
            return View(rule);
        }
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var rule = await _api.GetAccessRuleAsync(id);
        if (rule is null)
        {
            return NotFound();
        }

        await PopulateLists(rule.AccessPointId, rule.ScheduleId);
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
            await PopulateLists(rule.AccessPointId, rule.ScheduleId);
            return View(rule);
        }

        try
        {
            await _api.UpdateAccessRuleAsync(rule);
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            await PopulateLists(rule.AccessPointId, rule.ScheduleId);
            SetScreenError("Не удалось сохранить изменения правила доступа.", ex);
            return View(rule);
        }
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

    private async Task PopulateLists(Guid? selectedAccessPointId = null, Guid? selectedScheduleId = null)
    {
        var points = await _api.GetAccessPointsAsync();
        var schedules = await _api.GetSchedulesAsync();

        ViewBag.AccessPointId = new SelectList(points, "Id", "Name", selectedAccessPointId);
        ViewBag.ScheduleId = new SelectList(schedules, "Id", "Name", selectedScheduleId);
    }

    private static IEnumerable<AccessRule> Sort(IEnumerable<AccessRule> rules, string sort, bool desc)
    {
        Func<AccessRule, object?> keySelector = sort.ToLowerInvariant() switch
        {
            "schedule" => rule => rule.Schedule?.Name,
            "status" => rule => rule.IsActive,
            _ => rule => rule.AccessPoint?.Name
        };

        return desc
            ? rules.OrderByDescending(keySelector).ThenBy(rule => rule.AccessPoint?.Name)
            : rules.OrderBy(keySelector).ThenBy(rule => rule.AccessPoint?.Name);
    }
}