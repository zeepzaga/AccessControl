using AccessControl.Application.Access;
using Microsoft.Extensions.Logging;

namespace AccessControl.Infrastructure.Services;

public class LogOnlyDeviceResponseSender : IDeviceResponseSender
{
    private readonly ILogger<LogOnlyDeviceResponseSender> _logger;

    public LogOnlyDeviceResponseSender(ILogger<LogOnlyDeviceResponseSender> logger)
    {
        _logger = logger;
    }

    public Task SendDecisionAsync(Guid? deviceId, bool granted, string? message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Device response: deviceId={DeviceId}, granted={Granted}, message={Message}", deviceId, granted, message);
        return Task.CompletedTask;
    }
}
