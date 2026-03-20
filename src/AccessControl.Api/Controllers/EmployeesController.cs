using AccessControl.Domain.Entities;
using AccessControl.Infrastructure.Data;
using AccessControl.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AccessControl.Api.Controllers;

[ApiController]
[Route("api/employees")]
public class EmployeesController : ControllerBase
{
    private readonly AccessControlDbContext _db;
    private readonly LocalBiometricTemplateService _biometricTemplateService;

    public EmployeesController(AccessControlDbContext db, LocalBiometricTemplateService biometricTemplateService)
    {
        _db = db;
        _biometricTemplateService = biometricTemplateService;
    }

    [HttpGet]
    public async Task<ActionResult<List<Employee>>> GetAll([FromQuery] string? q = null, [FromQuery] bool? isActive = null, [FromQuery] string? department = null)
    {
        IQueryable<Employee> query = _db.Employees
            .Include(e => e.EmployeeDepartments)
            .ThenInclude(ed => ed.Department)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(e => EF.Functions.ILike(e.FullName, $"%{q}%"));
        }

        if (!string.IsNullOrWhiteSpace(department))
        {
            query = query.Where(e => e.EmployeeDepartments.Any(ed => ed.Department != null && EF.Functions.ILike(ed.Department.Name, $"%{department}%")));
        }

        if (isActive.HasValue)
        {
            query = query.Where(e => e.IsActive == isActive.Value);
        }

        var data = await query.OrderBy(e => e.FullName).ToListAsync();
        foreach (var employee in data)
        {
            PopulateDepartmentNames(employee);
        }

        return Ok(data);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Employee>> GetById(Guid id)
    {
        var employee = await _db.Employees
            .Include(e => e.Cards)
            .Include(e => e.EmployeeDepartments)
            .ThenInclude(ed => ed.Department)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee is null)
        {
            return NotFound();
        }

        PopulateDepartmentNames(employee);
        return Ok(employee);
    }

    [HttpPost]
    public async Task<ActionResult<Employee>> Create(EmployeeUpsertRequest request)
    {
        var employee = new Employee
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName.Trim(),
            IsActive = request.IsActive,
            FaceImage = request.FaceImage,
            CreatedAt = DateTime.UtcNow
        };

        PopulateBiometricData(employee);
        await ReplaceDepartmentsAsync(employee, request.DepartmentNamesInput, CancellationToken.None);
        PopulateDepartmentNames(employee);
        _db.Employees.Add(employee);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = employee.Id }, employee);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, EmployeeUpsertRequest request)
    {
        var existing = await _db.Employees
            .Include(e => e.EmployeeDepartments)
            .ThenInclude(ed => ed.Department)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (existing is null)
        {
            return NotFound();
        }

        existing.FullName = request.FullName.Trim();
        existing.IsActive = request.IsActive;
        existing.FaceImage = request.FaceImage;
        PopulateBiometricData(existing);
        await ReplaceDepartmentsAsync(existing, request.DepartmentNamesInput, CancellationToken.None);

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var existing = await _db.Employees.FindAsync(id);
        if (existing is null)
        {
            return NotFound();
        }

        _db.Employees.Remove(existing);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private void PopulateBiometricData(Employee employee)
    {
        if (employee.FaceImage is null || employee.FaceImage.Length == 0)
        {
            employee.FaceImage = null;
            employee.FaceEmbedding = null;
            employee.BiometricUpdatedAt = null;
            return;
        }

        var embedding = _biometricTemplateService.CreateEmbedding(employee.FaceImage);
        employee.FaceEmbedding = embedding;
        employee.BiometricUpdatedAt = embedding is null ? null : DateTime.UtcNow;
    }

    private async Task ReplaceDepartmentsAsync(Employee employee, string? departmentNamesInput, CancellationToken cancellationToken)
    {
        employee.EmployeeDepartments.Clear();

        var departmentNames = (departmentNamesInput ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(name => name.Trim())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (departmentNames.Count == 0)
        {
            return;
        }

        var existingDepartments = await _db.Departments
            .Where(d => departmentNames.Contains(d.Name))
            .ToListAsync(cancellationToken);

        foreach (var departmentName in departmentNames)
        {
            var department = existingDepartments.FirstOrDefault(d => string.Equals(d.Name, departmentName, StringComparison.OrdinalIgnoreCase));
            if (department is null)
            {
                department = new Department
                {
                    Id = Guid.NewGuid(),
                    Name = departmentName
                };
                _db.Departments.Add(department);
                existingDepartments.Add(department);
            }

            employee.EmployeeDepartments.Add(new EmployeeDepartment
            {
                EmployeeId = employee.Id,
                Employee = employee,
                DepartmentId = department.Id,
                Department = department
            });
        }
    }

    private static void PopulateDepartmentNames(Employee employee)
    {
        employee.DepartmentNames = employee.EmployeeDepartments
            .Where(ed => ed.Department != null)
            .Select(ed => ed.Department!.Name)
            .OrderBy(name => name)
            .ToList();
    }

    public sealed class EmployeeUpsertRequest
    {
        public string FullName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public byte[]? FaceImage { get; set; }
        public string? DepartmentNamesInput { get; set; }
    }
}
