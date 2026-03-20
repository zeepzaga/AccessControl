namespace AccessControl.Infrastructure.Security;

public interface IDeviceTokenService
{
    DeviceTokenResult CreateToken();
    string ComputeHash(string token);
}
