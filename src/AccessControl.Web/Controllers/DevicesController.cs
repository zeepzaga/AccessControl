using AccessControl.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace AccessControl.Web.Controllers;

public class DevicesController : AppController
{
    private readonly ApiClient _api;

    public DevicesController(ApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> Index([FromQuery] string? q = null, [FromQuery] bool? isActive = null)
    {
        var devices = await _api.GetDevicesAsync(q, isActive);
        ViewBag.Query = q;
        ViewBag.IsActive = isActive;
        return View(devices);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var device = await _api.GetDeviceAsync(id);
        if (device is null)
        {
            return NotFound();
        }

        return View(device);
    }

    public async Task<IActionResult> Create()
    {
        var model = new DeviceFormModel();
        await PopulateAccessPointOptionsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DeviceFormModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateAccessPointOptionsAsync(model);
            return View(model);
        }

        try
        {
            var created = await _api.CreateDeviceAsync(new ApiClient.DeviceUpsertRequest
            {
                Name = model.Name,
                Location = model.Location,
                AccessPointId = model.AccessPointId,
                IsActive = model.IsActive
            });

            TempData["GeneratedDeviceToken"] = created.Token;
            TempData["GeneratedDeviceTokenMessage"] = "Сохрани токен и передай его устройству. Повторно в открытом виде он больше не показывается.";
            return RedirectToAction(nameof(Details), new { id = created.Device.Id });
        }
        catch (Exception ex)
        {
            await PopulateAccessPointOptionsAsync(model);
            SetScreenError("Не удалось сохранить устройство.", ex);
            return View(model);
        }
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var device = await _api.GetDeviceAsync(id);
        if (device is null)
        {
            return NotFound();
        }

        var model = DeviceFormModel.FromDevice(device);
        await PopulateAccessPointOptionsAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, DeviceFormModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            await PopulateAccessPointOptionsAsync(model);
            return View(model);
        }

        try
        {
            await _api.UpdateDeviceAsync(id, new ApiClient.DeviceUpsertRequest
            {
                Name = model.Name,
                Location = model.Location,
                AccessPointId = model.AccessPointId,
                IsActive = model.IsActive
            });

            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception ex)
        {
            await PopulateAccessPointOptionsAsync(model);
            SetScreenError("Не удалось сохранить изменения устройства.", ex);
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RotateToken(Guid id)
    {
        var token = await _api.RotateDeviceTokenAsync(id);
        TempData["GeneratedDeviceToken"] = token.Token;
        TempData["GeneratedDeviceTokenMessage"] = "Токен перевыпущен. Старый токен больше не действует.";
        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Delete(Guid id)
    {
        var device = await _api.GetDeviceAsync(id);
        if (device is null)
        {
            return NotFound();
        }

        return View(device);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        await _api.DeleteDeviceAsync(id);
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateAccessPointOptionsAsync(DeviceFormModel model)
    {
        var accessPoints = await _api.GetAccessPointsAsync();
        model.AccessPointOptions = accessPoints
            .OrderBy(item => item.Name)
            .Select(item => new SelectableOptionViewModel
            {
                Id = item.Id,
                Label = string.IsNullOrWhiteSpace(item.Location) ? item.Name : $"{item.Name} ({item.Location})",
                Selected = item.Id == model.AccessPointId
            })
            .Prepend(new SelectableOptionViewModel
            {
                Id = Guid.Empty,
                Label = "Не привязано",
                Selected = model.AccessPointId is null
            })
            .ToList();
    }
}