using AccessControl.Application.Access;
using AccessControl.Domain.Entities;
using AccessControl.Infrastructure.Data;
using AccessControl.Infrastructure.Options;
using AccessControl.Infrastructure.Security;
using AccessControl.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AccessControl.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAccessControlInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("AccessControlDb")
            ?? "Host=localhost;Database=access_control;Username=postgres;Password=postgres";

        services.AddDbContext<AccessControlDbContext>(options =>
        {
            options.UseNpgsql(connectionString, o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
        });

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 8;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AccessControlDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        services.Configure<DeviceIntegrationOptions>(configuration.GetSection(DeviceIntegrationOptions.SectionName));
        services.Configure<BiometricOptions>(configuration.GetSection(BiometricOptions.SectionName));
        services.Configure<JwtAuthOptions>(configuration.GetSection(JwtAuthOptions.SectionName));
        services.Configure<BootstrapAdminOptions>(configuration.GetSection(BootstrapAdminOptions.SectionName));

        services.AddScoped<IAccessDecisionService, AccessDecisionService>();
        services.AddSingleton<LocalBiometricTemplateService>();
        services.AddScoped<LocalBiometricVerifier>();
        services.AddScoped<IBiometricVerifier>(sp => sp.GetRequiredService<LocalBiometricVerifier>());
        services.AddScoped<IDeviceResponseSender, LogOnlyDeviceResponseSender>();
        services.AddScoped<IAccessEventExporter, AccessEventExporter>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IRefreshTokenService, RefreshTokenService>();
        services.AddSingleton<IDeviceTokenService, DeviceTokenService>();
        services.AddScoped<IdentitySeeder>();

        return services;
    }
}
