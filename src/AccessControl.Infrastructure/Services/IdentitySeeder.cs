using AccessControl.Domain.Entities;
using AccessControl.Infrastructure.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace AccessControl.Infrastructure.Services;

public class IdentitySeeder
{
    public const string AdminRoleName = "Admin";

    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly BootstrapAdminOptions _options;

    public IdentitySeeder(
        RoleManager<IdentityRole<Guid>> roleManager,
        UserManager<ApplicationUser> userManager,
        IOptions<BootstrapAdminOptions> options)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _options = options.Value;
    }

    public async Task SeedAsync()
    {
        if (!await _roleManager.RoleExistsAsync(AdminRoleName))
        {
            await _roleManager.CreateAsync(new IdentityRole<Guid>(AdminRoleName));
        }

        var adminEmail = _options.Email.Trim();
        var user = await _userManager.FindByEmailAsync(adminEmail);

        if (user is null)
        {
            user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FullName = _options.FullName.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            var createResult = await _userManager.CreateAsync(user, _options.Password);
            if (!createResult.Succeeded)
            {
                var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to create bootstrap admin user: {errors}");
            }
        }

        if (!await _userManager.IsInRoleAsync(user, AdminRoleName))
        {
            await _userManager.AddToRoleAsync(user, AdminRoleName);
        }
    }
}
