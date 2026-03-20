using AccessControl.Domain.Entities;
using AccessControl.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccessControl.Api.Controllers;

[ApiController]
[Route("api/nfc-cards")]
public class NfcCardsController : ControllerBase
{
    private readonly AccessControlDbContext _db;

    public NfcCardsController(AccessControlDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<NfcCard>>> GetAll([FromQuery] string? q = null, [FromQuery] string? cardType = null, [FromQuery] bool? isActive = null, [FromQuery] Guid? employeeId = null)
    {
        var query = _db.NfcCards.Include(c => c.Employee).AsNoTracking();

        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(c => EF.Functions.ILike(c.Uid, $"%{q}%") || (c.Employee != null && EF.Functions.ILike(c.Employee.FullName, $"%{q}%")));
        }

        if (!string.IsNullOrWhiteSpace(cardType))
        {
            query = query.Where(c => c.CardType.ToString().Equals(cardType, StringComparison.OrdinalIgnoreCase));
        }

        if (isActive.HasValue)
        {
            query = query.Where(c => c.IsActive == isActive.Value);
        }

        if (employeeId.HasValue)
        {
            query = query.Where(c => c.EmployeeId == employeeId.Value);
        }

        var data = await query.OrderBy(c => c.Uid).ToListAsync();
        return Ok(data);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<NfcCard>> GetById(Guid id)
    {
        var card = await _db.NfcCards.Include(c => c.Employee).AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        if (card is null)
        {
            return NotFound();
        }

        return Ok(card);
    }

    [HttpPost]
    public async Task<ActionResult<NfcCard>> Create(NfcCard card)
    {
        card.Id = Guid.NewGuid();
        card.IssuedAt = DateTime.UtcNow;
        _db.NfcCards.Add(card);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = card.Id }, card);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, NfcCard card)
    {
        if (id != card.Id)
        {
            return BadRequest();
        }

        var existing = await _db.NfcCards.FindAsync(id);
        if (existing is null)
        {
            return NotFound();
        }

        existing.Uid = card.Uid;
        existing.EmployeeId = card.EmployeeId;
        existing.CardType = card.CardType;
        existing.ExpiresAt = card.ExpiresAt;
        existing.IsActive = card.IsActive;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var existing = await _db.NfcCards.FindAsync(id);
        if (existing is null)
        {
            return NotFound();
        }

        _db.NfcCards.Remove(existing);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
