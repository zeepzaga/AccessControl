using AccessControl.Application.Access;
using AccessControl.Infrastructure.Data;
using AccessControl.Infrastructure.Options;
using AccessControl.Infrastructure.Services;
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

        services.Configure<DeviceIntegrationOptions>(configuration.GetSection(DeviceIntegrationOptions.SectionName));
        services.Configure<BiometricOptions>(configuration.GetSection(BiometricOptions.SectionName));

        services.AddScoped<IAccessDecisionService, AccessDecisionService>();
        services.AddSingleton<LocalBiometricTemplateService>();
        services.AddScoped<LocalBiometricVerifier>();
        services.AddScoped<IBiometricVerifier>(sp => sp.GetRequiredService<LocalBiometricVerifier>());
        services.AddScoped<IDeviceResponseSender, LogOnlyDeviceResponseSender>();
        services.AddScoped<IAccessEventExporter, AccessEventExporter>();

        return services;
    }
}
