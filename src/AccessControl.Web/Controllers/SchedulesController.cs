using AccessControl.Domain.Entities;
using AccessControl.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace AccessControl.Web.Controllers;

public class SchedulesController : Controller
{
    private readonly ApiClient _api;

    public SchedulesController(ApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index(
        [FromQuery] string? q = null,
        [FromQuery] string sort = "name",
        [FromQuery] bool desc = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var schedules = await _api.GetSchedulesAsync(q);
        var model = PagedListViewModel<Schedule>.Create(Sort(schedules, sort, desc), page, pageSize, sort, desc);

        ViewBag.Query = q;
        ViewBag.Sort = model.Sort;
        ViewBag.Desc = model.Desc;
        ViewBag.PageSize = model.PageSize;
        return View(model);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var schedule = await _api.GetScheduleAsync(id);
        if (schedule is null)
        {
            return NotFound();
        }

        var rules = await _api.GetAccessRulesAsync(scheduleId: id);
        ViewBag.AccessRules = rules;
        return View(schedule);
    }

    public IActionResult Create()
    {
        return View(new Schedule
        {
            ScheduleJson = "{\n  \"timezone\": \"Europe/Moscow\",\n  \"defaultAccess\": false,\n  \"weeklyRules\": [\n    {\n      \"days\": [1,2,3,4,5],\n      \"intervals\": [\n        { \"from\": \"08:00\", \"to\": \"21:00\" }\n      ]\n    }\n  ],\n  \"dateOverrides\": []\n}"
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Schedule schedule)
    {
        if (!ModelState.IsValid)
        {
            return View(schedule);
        }

        await _api.CreateScheduleAsync(schedule);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var schedule = await _api.GetScheduleAsync(id);
        if (schedule is null)
        {
            return NotFound();
        }

        return View(schedule);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, Schedule schedule)
    {
        if (id != schedule.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(schedule);
        }

        await _api.UpdateScheduleAsync(schedule);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(Guid id)
    {
        var schedule = await _api.GetScheduleAsync(id);
        if (schedule is null)
        {
            return NotFound();
        }

        return View(schedule);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        await _api.DeleteScheduleAsync(id);
        return RedirectToAction(nameof(Index));
    }

    private static IEnumerable<Schedule> Sort(IEnumerable<Schedule> schedules, string sort, bool desc)
    {
        Func<Schedule, object?> keySelector = sort.ToLowerInvariant() switch
        {
            "created" => schedule => schedule.CreatedAt,
            _ => schedule => schedule.Name
        };

        return desc
            ? schedules.OrderByDescending(keySelector).ThenBy(schedule => schedule.Name)
            : schedules.OrderBy(keySelector).ThenBy(schedule => schedule.Name);
    }
}
