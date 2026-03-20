using System;
using System.Threading;
using System.Threading.Tasks;

namespace AccessControl.Application.Access;

public interface IBiometricVerifier
{
    Task<bool> VerifyAsync(Guid employeeId, byte[]? faceImage, CancellationToken cancellationToken = default);
}
