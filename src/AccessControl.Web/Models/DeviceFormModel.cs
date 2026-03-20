using System.ComponentModel.DataAnnotations;

namespace AccessControl.Web.Models;

public class DeviceFormModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "Укажи название устройства.")]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Location { get; set; }

    public Guid? AccessPointId { get; set; }

    public bool IsActive { get; set; } = true;

    public List<SelectableOptionViewModel> AccessPointOptions { get; set; } = new();

    public static DeviceFormModel FromDevice(ApiClient.DeviceResponse device)
    {
        return new DeviceFormModel
        {
            Id = device.Id,
            Name = device.Name,
            Location = device.Location,
            AccessPointId = device.AccessPointId,
            IsActive = device.IsActive
        };
    }
}
