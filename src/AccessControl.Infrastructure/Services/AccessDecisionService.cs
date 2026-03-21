using AccessControl.Application.Access;
using AccessControl.Domain.Enums;
using AccessControl.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AccessControl.Infrastructure.Services;

public class AccessDecisionService : IAccessDecisionService
{
    private readonly AccessControlDbContext _db;
    private readonly IBiometricVerifier _biometricVerifier;

    public AccessDecisionService(AccessControlDbContext db, IBiometricVerifier biometricVerifier)
    {
        _db = db;
        _biometricVerifier = biometricVerifier;
    }

    public async Task<AccessDecision> ProcessCardReadAsync(CardReadRequest request, CancellationToken cancellationToken = default)
    {
        var nowUtc = request.EventTimeUtc;

        var card = await _db.NfcCards
            .Include(c => c.Employee)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Uid == request.CardUid, cancellationToken);

        if (card is null)
        {
            await LogEventAsync(request, null, false, AccessEventReason.CardNotFound, cancellationToken);
            return new AccessDecision(false, AccessEventReason.CardNotFound, null, null, null);
        }

        if (!card.IsActive)
        {
            await LogEventAsync(request, card.EmployeeId, false, AccessEventReason.AccessDenied, cancellationToken);
            return new AccessDecision(false, AccessEventReason.AccessDenied, card.EmployeeId, card.Employee?.FullName, card.CardType);
        }

        if (card.ExpiresAt.HasValue && card.ExpiresAt.Value.ToUniversalTime() < nowUtc)
        {
            await LogEventAsync(request, card.EmployeeId, false, AccessEventReason.CardExpired, cancellationToken);
            return new AccessDecision(false, AccessEventReason.CardExpired, card.EmployeeId, card.Employee?.FullName, card.CardType);
        }

        if (card.EmployeeId is null)
        {
            if (card.CardType == CardType.Guest && await IsGuestAccessAllowedAsync(request.AccessPointId, cancellationToken))
            {
                await LogEventAsync(request, null, true, AccessEventReason.OK, cancellationToken);
                return new AccessDecision(true, AccessEventReason.OK, null, null, card.CardType);
            }

            await LogEventAsync(request, null, false, AccessEventReason.AccessDenied, cancellationToken);
            return new AccessDecision(false, AccessEventReason.AccessDenied, null, null, card.CardType);
        }

        var (allowed, denyReason) = await CheckAccessRulesAsync(card.EmployeeId.Value, request.AccessPointId, nowUtc, cancellationToken);
        if (!allowed)
        {
            await LogEventAsync(request, card.EmployeeId, false, denyReason, cancellationToken);
            return new AccessDecision(false, denyReason, card.EmployeeId, card.Employee?.FullName, card.CardType);
        }

        if (card.CardType != CardType.Guest)
        {
            var biometricOk = await _biometricVerifier.VerifyAsync(card.EmployeeId.Value, request.FaceImage, cancellationToken);
            if (!biometricOk)
            {
                await LogEventAsync(request, card.EmployeeId, false, AccessEventReason.BiometricFailed, cancellationToken);
                return new AccessDecision(false, AccessEventReason.BiometricFailed, card.EmployeeId, card.Employee?.FullName, card.CardType);
            }
        }

        await LogEventAsync(request, card.EmployeeId, true, AccessEventReason.OK, cancellationToken);
        return new AccessDecision(true, AccessEventReason.OK, card.EmployeeId, card.Employee?.FullName, card.CardType);
    }

    private async Task<bool> IsGuestAccessAllowedAsync(Guid? accessPointId, CancellationToken cancellationToken)
    {
        if (!accessPointId.HasValue)
        {
            return false;
        }

        return await _db.AccessPoints
            .AsNoTracking()
            .AnyAsync(p => p.Id == accessPointId.Value && p.IsActive && p.IsGuestAccess, cancellationToken);
    }

    private async Task<(bool Allowed, AccessEventReason DenyReason)> CheckAccessRulesAsync(Guid employeeId, Guid? accessPointId, DateTime eventTimeUtc, CancellationToken cancellationToken)
    {
        if (!accessPointId.HasValue)
        {
            return (false, AccessEventReason.AccessDenied);
        }

        var directAccess = await _db.EmployeeAccessPoints
            .AsNoTracking()
            .AnyAsync(link => link.AccessPointId == accessPointId.Value && link.EmployeeId == employeeId, cancellationToken);

        var departmentAccess = await _db.DepartmentAccessPoints
            .AsNoTracking()
            .AnyAsync(link => link.AccessPointId == accessPointId.Value
                && _db.EmployeeDepartments.Any(ed => ed.EmployeeId == employeeId && ed.DepartmentId == link.DepartmentId), cancellationToken);

        if (!directAccess && !departmentAccess)
        {
            return (false, AccessEventReason.AccessDenied);
        }

        var rules = await _db.AccessRules
            .Include(r => r.Schedule)
            .AsNoTracking()
            .Where(r => r.IsActive && r.AccessPointId == accessPointId.Value)
            .ToListAsync(cancellationToken);

        if (rules.Count == 0)
        {
            return (true, AccessEventReason.OK);
        }

        var sawScheduleDenied = false;

        foreach (var rule in rules)
        {
            if (rule.ValidFrom.HasValue && rule.ValidFrom.Value.ToUniversalTime() > eventTimeUtc)
            {
                continue;
            }

            if (rule.ValidTo.HasValue && rule.ValidTo.Value.ToUniversalTime() < eventTimeUtc)
            {
                continue;
            }

            if (rule.Schedule is null)
            {
                return (true, AccessEventReason.OK);
            }

            if (IsScheduleAllowed(rule.Schedule.ScheduleJson, eventTimeUtc))
            {
                return (true, AccessEventReason.OK);
            }

            sawScheduleDenied = true;
        }

        return (false, sawScheduleDenied ? AccessEventReason.ScheduleDenied : AccessEventReason.AccessDenied);
    }

    private static bool IsScheduleAllowed(string scheduleJson, DateTime eventTimeUtc)
    {
        if (string.IsNullOrWhiteSpace(scheduleJson))
        {
            return true;
        }

        try
        {
            var schedule = System.Text.Json.JsonSerializer.Deserialize<ScheduleDefinition>(scheduleJson, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (schedule is null)
            {
                return true;
            }

            TimeZoneInfo tz;
            try
            {
                tz = TimeZoneInfo.FindSystemTimeZoneById(schedule.Timezone ?? "UTC");
            }
            catch
            {
                tz = TimeZoneInfo.Utc;
            }

            var localTime = TimeZoneInfo.ConvertTimeFromUtc(eventTimeUtc, tz);
            var localDate = localTime.Date;

            if (schedule.DateOverrides is not null)
            {
                var overrideMatch = schedule.DateOverrides.FirstOrDefault(o => o.Date.Date == localDate);
                if (overrideMatch is not null)
                {
                    return overrideMatch.Access;
                }
            }

            if (schedule.WeeklyRules is null || schedule.WeeklyRules.Count == 0)
            {
                return schedule.DefaultAccess;
            }

            var day = IsoDayOfWeek(localTime.DayOfWeek);
            foreach (var rule in schedule.WeeklyRules)
            {
                if (rule.Days is null || !rule.Days.Contains(day))
                {
                    continue;
                }

                if (rule.Intervals is null || rule.Intervals.Count == 0)
                {
                    continue;
                }

                foreach (var interval in rule.Intervals)
                {
                    if (TimeSpan.TryParse(interval.From, out var from) && TimeSpan.TryParse(interval.To, out var to))
                    {
                        var time = localTime.TimeOfDay;
                        if (time >= from && time <= to)
                        {
                            return true;
                        }
                    }
                }
            }

            return schedule.DefaultAccess;
        }
        catch
        {
            return true;
        }
    }

    private static int IsoDayOfWeek(DayOfWeek dayOfWeek)
    {
        return dayOfWeek == DayOfWeek.Sunday ? 7 : (int)dayOfWeek;
    }

    private async Task LogEventAsync(CardReadRequest request, Guid? employeeId, bool granted, AccessEventReason reason, CancellationToken cancellationToken)
    {
        var evt = new Domain.Entities.AccessEvent
        {
            Id = Guid.NewGuid(),
            DeviceId = request.DeviceId,
            AccessPointId = request.AccessPointId,
            CardUid = request.CardUid,
            EmployeeId = employeeId,
            EventTime = request.EventTimeUtc,
            AccessGranted = granted,
            Reason = reason
        };

        _db.AccessEvents.Add(evt);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private sealed class ScheduleDefinition
    {
        public string? Timezone { get; set; }
        public bool DefaultAccess { get; set; }
        public List<WeeklyRule>? WeeklyRules { get; set; }
        public List<DateOverride>? DateOverrides { get; set; }
    }

    private sealed class WeeklyRule
    {
        public List<int>? Days { get; set; }
        public List<TimeInterval>? Intervals { get; set; }
    }

    private sealed class TimeInterval
    {
        public string? From { get; set; }
        public string? To { get; set; }
    }

    private sealed class DateOverride
    {
        public DateTime Date { get; set; }
        public bool Access { get; set; }
    }
}
