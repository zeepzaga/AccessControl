using AccessControl.Domain.Entities;
using AccessControl.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace AccessControl.Web.Controllers;

public class AccessPointsController : AppController
{
    private readonly ApiClient _api;

    public AccessPointsController(ApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index(
        [FromQuery] string? q = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string sort = "name",
        [FromQuery] bool desc = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var points = await _api.GetAccessPointsAsync(q, isActive);
        var model = PagedListViewModel<AccessPoint>.Create(Sort(points, sort, desc), page, pageSize, sort, desc);

        ViewBag.Query = q;
        ViewBag.IsActive = isActive;
        ViewBag.Sort = model.Sort;
        ViewBag.Desc = model.Desc;
        ViewBag.PageSize = model.PageSize;
        return View(model);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var point = await _api.GetAccessPointAsync(id);
        if (point is null)
        {
            return NotFound();
        }

        var rules = await _api.GetAccessRulesAsync(accessPointId: id);
        var eventsList = await _api.GetAccessEventsAsync(50, accessPointId: id);
        ViewBag.AccessRules = rules;
        ViewBag.RecentEvents = eventsList.Take(10).ToList();
        return View(point);
    }

    public async Task<IActionResult> Create()
    {
        var model = new AccessPointFormModel();
        await PopulateSelectionsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AccessPointFormModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateSelectionsAsync(model);
            return View(model);
        }

        try
        {
            await _api.CreateAccessPointAsync(new ApiClient.AccessPointUpsertRequest
            {
                Name = model.Name,
                Location = model.Location,
                IsActive = model.IsActive,
                IsGuestAccess = model.IsGuestAccess,
                SelectedEmployeeIds = model.SelectedEmployeeIds,
                SelectedDepartmentIds = model.SelectedDepartmentIds
            });

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            await PopulateSelectionsAsync(model);
            SetScreenError("Не удалось сохранить точку доступа.", ex);
            return View(model);
        }
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var point = await _api.GetAccessPointAsync(id);
        if (point is null)
        {
            return NotFound();
        }

        var rules = await _api.GetAccessRulesAsync(accessPointId: id);
        var model = AccessPointFormModel.FromAccessPoint(point, rules);
        model.SelectedDepartmentIds = await ResolveDepartmentIdsAsync(point.DepartmentNames);
        model.DepartmentNames = point.DepartmentNames;
        await PopulateSelectionsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, AccessPointFormModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            model.ExistingRules = await _api.GetAccessRulesAsync(accessPointId: id);
            await PopulateSelectionsAsync(model);
            return View(model);
        }

        try
        {
            await _api.UpdateAccessPointAsync(id, new ApiClient.AccessPointUpsertRequest
            {
                Name = model.Name,
                Location = model.Location,
                IsActive = model.IsActive,
                IsGuestAccess = model.IsGuestAccess,
                SelectedEmployeeIds = model.SelectedEmployeeIds,
                SelectedDepartmentIds = model.SelectedDepartmentIds
            });

            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception ex)
        {
            model.ExistingRules = await _api.GetAccessRulesAsync(accessPointId: id);
            await PopulateSelectionsAsync(model);
            SetScreenError("Не удалось сохранить изменения точки доступа.", ex);
            return View(model);
        }
    }

    public async Task<IActionResult> Delete(Guid id)
    {
        var point = await _api.GetAccessPointAsync(id);
        if (point is null)
        {
            return NotFound();
        }

        return View(point);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        await _api.DeleteAccessPointAsync(id);
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateSelectionsAsync(AccessPointFormModel model)
    {
        var employees = await _api.GetEmployeesAsync(isActive: true);
        var departments = await _api.GetDepartmentsAsync();
        var selectedEmployeeIds = model.SelectedEmployeeIds.ToHashSet();
        var selectedDepartmentIds = model.SelectedDepartmentIds.ToHashSet();

        model.AvailableEmployees = employees
            .OrderBy(employee => employee.FullName)
            .Select(employee => new SelectableOptionViewModel
            {
                Id = employee.Id,
                Label = employee.FullName,
                SearchText = employee.FullName,
                Selected = selectedEmployeeIds.Contains(employee.Id)
            })
            .ToList();

        model.AvailableDepartments = departments
            .OrderBy(department => department.Name)
            .Select(department => new SelectableOptionViewModel
            {
                Id = department.Id,
                Label = department.Name,
                SearchText = department.Name,
                Selected = selectedDepartmentIds.Contains(department.Id)
            })
            .ToList();
    }

    private async Task<List<Guid>> ResolveDepartmentIdsAsync(IEnumerable<string>? departmentNames)
    {
        var names = departmentNames?
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];

        if (names.Count == 0)
        {
            return [];
        }

        var departments = await _api.GetDepartmentsAsync();
        return departments
            .Where(department => names.Contains(department.Name))
            .Select(department => department.Id)
            .ToList();
    }

    private static IEnumerable<AccessPoint> Sort(IEnumerable<AccessPoint> points, string sort, bool desc)
    {
        Func<AccessPoint, object?> keySelector = sort.ToLowerInvariant() switch
        {
            "location" => point => point.Location,
            "departments" => point => string.Join(", ", point.DepartmentNames),
            "guest" => point => point.IsGuestAccess,
            "status" => point => point.IsActive,
            _ => point => point.Name
        };

        return desc
            ? points.OrderByDescending(keySelector).ThenBy(point => point.Name)
            : points.OrderBy(keySelector).ThenBy(point => point.Name);
    }
}