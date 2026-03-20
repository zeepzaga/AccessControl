using AccessControl.Domain.Entities;
using AccessControl.Infrastructure.Data;
using AccessControl.Infrastructure.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccessControl.Api.Controllers;

[ApiController]
[Route("api/devices")]
public class DevicesController : ControllerBase
{
    private readonly AccessControlDbContext _db;
    private readonly IDeviceTokenService _deviceTokenService;

    public DevicesController(AccessControlDbContext db, IDeviceTokenService deviceTokenService)
    {
        _db = db;
        _deviceTokenService = deviceTokenService;
    }

    [HttpGet]
    public async Task<ActionResult<List<DeviceResponse>>> GetAll([FromQuery] string? q = null, [FromQuery] bool? isActive = null)
    {
        IQueryable<Device> query = _db.Devices
            .Include(device => device.AccessPoint)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(device => EF.Functions.ILike(device.Name, $"%{q}%")
                || (device.Location != null && EF.Functions.ILike(device.Location, $"%{q}%")));
        }

        if (isActive.HasValue)
        {
            query = query.Where(device => device.IsActive == isActive.Value);
        }

        var devices = await query.OrderBy(device => device.Name).ToListAsync();
        return Ok(devices.Select(MapResponse).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DeviceResponse>> GetById(Guid id)
    {
        var device = await _db.Devices
            .Include(item => item.AccessPoint)
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id);

        if (device is null)
        {
            return NotFound();
        }

        return Ok(MapResponse(device));
    }

    [HttpPost]
    public async Task<ActionResult<DeviceWithTokenResponse>> Create([FromBody] DeviceUpsertRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "name is required" });
        }

        var token = _deviceTokenService.CreateToken();
        var device = new Device
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Location = string.IsNullOrWhiteSpace(request.Location) ? null : request.Location.Trim(),
            AccessPointId = request.AccessPointId,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow,
            TokenHash = token.TokenHash,
            TokenHint = token.TokenHint,
            TokenLastRotatedAt = token.RotatedAtUtc
        };

        _db.Devices.Add(device);
        await _db.SaveChangesAsync();

        device = await _db.Devices.Include(item => item.AccessPoint).FirstAsync(item => item.Id == device.Id);

        return CreatedAtAction(nameof(GetById), new { id = device.Id }, new DeviceWithTokenResponse
        {
            Device = MapResponse(device),
            Token = token.PlainTextToken
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] DeviceUpsertRequest request)
    {
        var device = await _db.Devices.FirstOrDefaultAsync(item => item.Id == id);
        if (device is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "name is required" });
        }

        device.Name = request.Name.Trim();
        device.Location = string.IsNullOrWhiteSpace(request.Location) ? null : request.Location.Trim();
        device.AccessPointId = request.AccessPointId;
        device.IsActive = request.IsActive;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id:guid}/rotate-token")]
    public async Task<ActionResult<DeviceTokenResponse>> RotateToken(Guid id)
    {
        var device = await _db.Devices.FirstOrDefaultAsync(item => item.Id == id);
        if (device is null)
        {
            return NotFound();
        }

        var token = _deviceTokenService.CreateToken();
        device.TokenHash = token.TokenHash;
        device.TokenHint = token.TokenHint;
        device.TokenLastRotatedAt = token.RotatedAtUtc;

        await _db.SaveChangesAsync();

        return Ok(new DeviceTokenResponse
        {
            DeviceId = device.Id,
            DeviceName = device.Name,
            Token = token.PlainTextToken,
            TokenHint = token.TokenHint,
            RotatedAtUtc = token.RotatedAtUtc
        });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var device = await _db.Devices.FirstOrDefaultAsync(item => item.Id == id);
        if (device is null)
        {
            return NotFound();
        }

        _db.Devices.Remove(device);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private static DeviceResponse MapResponse(Device device)
    {
        return new DeviceResponse
        {
            Id = device.Id,
            Name = device.Name,
            Location = device.Location,
            AccessPointId = device.AccessPointId,
            AccessPointName = device.AccessPoint?.Name,
            TokenHint = device.TokenHint,
            TokenLastRotatedAt = device.TokenLastRotatedAt,
            IsActive = device.IsActive,
            CreatedAt = device.CreatedAt
        };
    }

    public sealed class DeviceUpsertRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Location { get; set; }
        public Guid? AccessPointId { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public sealed class DeviceResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Location { get; set; }
        public Guid? AccessPointId { get; set; }
        public string? AccessPointName { get; set; }
        public string? TokenHint { get; set; }
        public DateTime? TokenLastRotatedAt { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public sealed class DeviceWithTokenResponse
    {
        public DeviceResponse Device { get; set; } = new();
        public string Token { get; set; } = string.Empty;
    }

    public sealed class DeviceTokenResponse
    {
        public Guid DeviceId { get; set; }
        public string DeviceName { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string TokenHint { get; set; } = string.Empty;
        public DateTime RotatedAtUtc { get; set; }
    }
}
