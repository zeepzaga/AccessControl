using AccessControl.Domain.Entities;
using AccessControl.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccessControl.Api.Controllers;

[ApiController]
[Route("api/access-rules")]
public class AccessRulesController : ControllerBase
{
    private readonly AccessControlDbContext _db;

    public AccessRulesController(AccessControlDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<AccessRule>>> GetAll([FromQuery] Guid? accessPointId = null, [FromQuery] Guid? scheduleId = null, [FromQuery] bool? isActive = null)
    {
        IQueryable<AccessRule> query = _db.AccessRules
            .Include(r => r.AccessPoint)
            .Include(r => r.Schedule)
            .AsNoTracking();

        if (accessPointId.HasValue)
        {
            query = query.Where(r => r.AccessPointId == accessPointId.Value);
        }

        if (scheduleId.HasValue)
        {
            query = query.Where(r => r.ScheduleId == scheduleId.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(r => r.IsActive == isActive.Value);
        }

        var data = await query.ToListAsync();
        return Ok(data);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AccessRule>> GetById(Guid id)
    {
        var rule = await _db.AccessRules
            .Include(r => r.AccessPoint)
            .Include(r => r.Schedule)
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id);

        if (rule is null)
        {
            return NotFound();
        }

        return Ok(rule);
    }

    [HttpPost]
    public async Task<ActionResult<AccessRule>> Create(AccessRule rule)
    {
        rule.Id = Guid.NewGuid();
        _db.AccessRules.Add(rule);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = rule.Id }, rule);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, AccessRule rule)
    {
        if (id != rule.Id)
        {
            return BadRequest();
        }

        var existing = await _db.AccessRules.FindAsync(id);
        if (existing is null)
        {
            return NotFound();
        }

        existing.AccessPointId = rule.AccessPointId;
        existing.ScheduleId = rule.ScheduleId;
        existing.ValidFrom = rule.ValidFrom;
        existing.ValidTo = rule.ValidTo;
        existing.IsActive = rule.IsActive;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var existing = await _db.AccessRules.FindAsync(id);
        if (existing is null)
        {
            return NotFound();
        }

        _db.AccessRules.Remove(existing);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
