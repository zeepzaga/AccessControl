using AccessControl.Application.Access;
using AccessControl.Infrastructure.Data;
using AccessControl.Infrastructure.Options;
using AccessControl.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AccessControl.Api.Controllers;

[ApiController]
[Authorize(Policy = AuthPolicies.Device)]
[Route("api/device")]
public class DeviceController : ControllerBase
{
    private readonly IAccessDecisionService _decisionService;
    private readonly IDeviceResponseSender _responseSender;
    private readonly IOptions<DeviceIntegrationOptions> _options;
    private readonly AccessControlDbContext _db;

    public DeviceController(
        IAccessDecisionService decisionService,
        IDeviceResponseSender responseSender,
        IOptions<DeviceIntegrationOptions> options,
        AccessControlDbContext db)
    {
        _decisionService = decisionService;
        _responseSender = responseSender;
        _options = options;
        _db = db;
    }

    [HttpPost("card-read")]
    public async Task<IActionResult> CardRead([FromBody] CardReadMessage message, CancellationToken cancellationToken)
    {
        if (!string.Equals(_options.Value.Mode, "Http", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCode(StatusCodes.Status409Conflict, new { message = "HTTP mode disabled" });
        }

        if (string.IsNullOrWhiteSpace(message.CardUid))
        {
            return BadRequest(new { message = "cardUid is required" });
        }

        if (!Guid.TryParse(User.FindFirst("device_id")?.Value, out var authenticatedDeviceId))
        {
            return Forbid();
        }

        if (message.DeviceId.HasValue && message.DeviceId.Value != authenticatedDeviceId)
        {
            return Forbid();
        }

        var device = await _db.Devices.AsNoTracking().FirstOrDefaultAsync(d => d.Id == authenticatedDeviceId, cancellationToken);
        if (device is null || !device.IsActive)
        {
            return Unauthorized(new { message = "device not found or inactive" });
        }

        if (!device.AccessPointId.HasValue)
        {
            return BadRequest(new { message = "device has no access point" });
        }

        var faceImage = string.IsNullOrWhiteSpace(message.FaceImageBase64)
            ? null
            : Convert.FromBase64String(message.FaceImageBase64);

        var request = new CardReadRequest(
            message.CardUid,
            device.AccessPointId,
            authenticatedDeviceId,
            faceImage,
            DateTime.UtcNow);

        var decision = await _decisionService.ProcessCardReadAsync(request, cancellationToken);

        await _responseSender.SendDecisionAsync(authenticatedDeviceId, decision.Granted, decision.Reason.ToString(), cancellationToken);

        var response = new CardReadResponse
        {
            CardUid = message.CardUid,
            DeviceId = authenticatedDeviceId,
            AccessPointId = device.AccessPointId,
            Granted = decision.Granted,
            Reason = decision.Reason.ToString(),
            EmployeeId = decision.EmployeeId,
            EmployeeName = decision.EmployeeName
        };

        return Ok(response);
    }

    public sealed class CardReadMessage
    {
        public string CardUid { get; set; } = string.Empty;
        public Guid? DeviceId { get; set; }
        public string? FaceImageBase64 { get; set; }
    }

    public sealed class CardReadResponse
    {
        public string CardUid { get; set; } = string.Empty;
        public Guid? DeviceId { get; set; }
        public Guid? AccessPointId { get; set; }
        public bool Granted { get; set; }
        public string Reason { get; set; } = string.Empty;
        public Guid? EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
    }
}
