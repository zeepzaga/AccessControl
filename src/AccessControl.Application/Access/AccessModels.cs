using System;
using AccessControl.Domain.Enums;

namespace AccessControl.Application.Access;

public record CardReadRequest(
    string CardUid,
    Guid? AccessPointId,
    Guid? DeviceId,
    byte[]? FaceImage,
    DateTime EventTimeUtc);

public record AccessDecision(
    bool Granted,
    AccessEventReason Reason,
    Guid? EmployeeId,
    string? EmployeeName,
    CardType? CardType);
