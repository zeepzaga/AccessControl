namespace AccessControl.Domain.Enums;

public enum AccessEventReason
{
    OK = 0,
    CardNotFound = 1,
    AccessDenied = 2,
    ScheduleDenied = 3,
    CardExpired = 4,
    DeviceUnknown = 5,
    BiometricFailed = 6
}
