using System;
using System.Threading;
using System.Threading.Tasks;

namespace AccessControl.Application.Access;

public interface IDeviceResponseSender
{
    Task SendDecisionAsync(Guid? deviceId, bool granted, string? message, CancellationToken cancellationToken = default);
}
