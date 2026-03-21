using AccessControl.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace AccessControl.Web.Controllers;

public class EmployeesController : AppController
{
    private readonly ApiClient _api;

    public EmployeesController(ApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index(
        [FromQuery] string? q = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? department = null,
        [FromQuery] string sort = "fullname",
        [FromQuery] bool desc = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25)
    {
        var employees = await _api.GetEmployeesAsync(q, isActive, department);
        var departments = await _api.GetDepartmentsAsync();
        var model = PagedListViewModel<AccessControl.Domain.Entities.Employee>.Create(Sort(employees, sort, desc), page, pageSize, sort, desc);

        ViewBag.Query = q;
        ViewBag.IsActive = isActive;
        ViewBag.Department = department;
        ViewBag.DepartmentOptions = departments
            .OrderBy(item => item.Name)
            .Select(item => new SearchableSelectOptionViewModel
            {
                Value = item.Name,
                Label = item.Name,
                SearchText = item.Name,
                Selected = string.Equals(item.Name, department, StringComparison.OrdinalIgnoreCase)
            })
            .ToList();
        ViewBag.Sort = model.Sort;
        ViewBag.Desc = model.Desc;
        ViewBag.PageSize = model.PageSize;
        return View(model);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var employee = await _api.GetEmployeeAsync(id);
        if (employee is null)
        {
            return NotFound();
        }

        return View(employee);
    }

    public IActionResult Create()
    {
        return View(new EmployeeFormModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EmployeeFormModel model, IFormFile? facePhoto)
    {
        await PopulateFaceImageAsync(model, facePhoto);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _api.CreateEmployeeAsync(new ApiClient.EmployeeUpsertRequest
            {
                FullName = model.FullName,
                IsActive = model.IsActive,
                FaceImage = model.FaceImage,
                DepartmentNamesInput = model.DepartmentNamesInput
            });

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            SetScreenError("Не удалось сохранить сотрудника.", ex);
            return View(model);
        }
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var employee = await _api.GetEmployeeAsync(id);
        if (employee is null)
        {
            return NotFound();
        }

        return View(EmployeeFormModel.FromEmployee(employee));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, EmployeeFormModel model, IFormFile? facePhoto)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        var existing = await _api.GetEmployeeAsync(id);
        if (existing is null)
        {
            return NotFound();
        }

        await PopulateFaceImageAsync(model, facePhoto);

        if (!ModelState.IsValid)
        {
            model.FaceImage = existing.FaceImage;
            model.FaceEmbedding = existing.FaceEmbedding;
            model.BiometricUpdatedAt = existing.BiometricUpdatedAt;
            return View(model);
        }

        var faceImage = model.FaceImage is { Length: > 0 } ? model.FaceImage : existing.FaceImage;

        try
        {
            await _api.UpdateEmployeeAsync(id, new ApiClient.EmployeeUpsertRequest
            {
                FullName = model.FullName,
                IsActive = model.IsActive,
                FaceImage = faceImage,
                DepartmentNamesInput = model.DepartmentNamesInput
            });

            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception ex)
        {
            model.FaceImage = existing.FaceImage;
            model.FaceEmbedding = existing.FaceEmbedding;
            model.BiometricUpdatedAt = existing.BiometricUpdatedAt;
            SetScreenError("Не удалось сохранить изменения сотрудника.", ex);
            return View(model);
        }
    }

    public async Task<IActionResult> Delete(Guid id)
    {
        var employee = await _api.GetEmployeeAsync(id);
        if (employee is null)
        {
            return NotFound();
        }

        return View(employee);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        await _api.DeleteEmployeeAsync(id);
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateFaceImageAsync(EmployeeFormModel model, IFormFile? facePhoto)
    {
        if (facePhoto is null || facePhoto.Length == 0)
        {
            return;
        }

        if (facePhoto.Length > 5 * 1024 * 1024)
        {
            ModelState.AddModelError(nameof(model.FaceImage), "Фотография должна быть не больше 5 МБ.");
            return;
        }

        if (facePhoto.ContentType is null || !facePhoto.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(model.FaceImage), "Нужно загрузить изображение.");
            return;
        }

        await using var stream = new MemoryStream();
        await facePhoto.CopyToAsync(stream);
        model.FaceImage = stream.ToArray();
    }

    private static IEnumerable<AccessControl.Domain.Entities.Employee> Sort(IEnumerable<AccessControl.Domain.Entities.Employee> employees, string sort, bool desc)
    {
        Func<AccessControl.Domain.Entities.Employee, object?> keySelector = sort.ToLowerInvariant() switch
        {
            "departments" => employee => string.Join(", ", employee.DepartmentNames),
            "status" => employee => employee.IsActive,
            _ => employee => employee.FullName
        };

        return desc
            ? employees.OrderByDescending(keySelector).ThenBy(employee => employee.FullName)
            : employees.OrderBy(keySelector).ThenBy(employee => employee.FullName);
    }
}