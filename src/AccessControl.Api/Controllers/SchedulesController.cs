using AccessControl.Domain.Entities;
using AccessControl.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccessControl.Api.Controllers;

[ApiController]
[Route("api/schedules")]
public class SchedulesController : ControllerBase
{
    private readonly AccessControlDbContext _db;

    public SchedulesController(AccessControlDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<Schedule>>> GetAll([FromQuery] string? q = null)
    {
        var query = _db.Schedules.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(s => EF.Functions.ILike(s.Name, $"%{q}%"));
        }

        var data = await query.OrderBy(s => s.Name).ToListAsync();
        return Ok(data);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Schedule>> GetById(Guid id)
    {
        var schedule = await _db.Schedules.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
        if (schedule is null)
        {
            return NotFound();
        }

        return Ok(schedule);
    }

    [HttpPost]
    public async Task<ActionResult<Schedule>> Create(Schedule schedule)
    {
        schedule.Id = Guid.NewGuid();
        schedule.CreatedAt = DateTime.UtcNow;
        _db.Schedules.Add(schedule);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = schedule.Id }, schedule);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, Schedule schedule)
    {
        if (id != schedule.Id)
        {
            return BadRequest();
        }

        var existing = await _db.Schedules.FindAsync(id);
        if (existing is null)
        {
            return NotFound();
        }

        existing.Name = schedule.Name;
        existing.ScheduleJson = schedule.ScheduleJson;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var existing = await _db.Schedules.FindAsync(id);
        if (existing is null)
        {
            return NotFound();
        }

        _db.Schedules.Remove(existing);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
