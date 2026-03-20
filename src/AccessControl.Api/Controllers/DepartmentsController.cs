using AccessControl.Domain.Entities;
using AccessControl.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccessControl.Api.Controllers;

[ApiController]
[Route("api/departments")]
public class DepartmentsController : ControllerBase
{
    private readonly AccessControlDbContext _db;

    public DepartmentsController(AccessControlDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<Department>>> GetAll([FromQuery] string? q = null)
    {
        var query = _db.Departments.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(d => EF.Functions.ILike(d.Name, $"%{q}%"));
        }

        var data = await query.OrderBy(d => d.Name).ToListAsync();
        return Ok(data);
    }
}
