using System.Net.Http.Json;
using AccessControl.Domain.Entities;

namespace AccessControl.Web;

public class ApiClient
{
    private readonly HttpClient _http;

    public ApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<Employee>> GetEmployeesAsync(string? q = null, bool? isActive = null, string? department = null) =>
        await _http.GetFromJsonAsync<List<Employee>>("api/employees" + BuildQuery(new Dictionary<string, string?>
        {
            ["q"] = q,
            ["isActive"] = isActive?.ToString(),
            ["department"] = department
        })) ?? [];

    public async Task<Employee?> GetEmployeeAsync(Guid id) =>
        await _http.GetFromJsonAsync<Employee>($"api/employees/{id}");

    public async Task CreateEmployeeAsync(EmployeeUpsertRequest employee)
    {
        var response = await _http.PostAsJsonAsync("api/employees", employee);
        response.EnsureSuccessStatusCode();
    }

    public async Task UpdateEmployeeAsync(Guid id, EmployeeUpsertRequest employee)
    {
        var response = await _http.PutAsJsonAsync($"api/employees/{id}", employee);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteEmployeeAsync(Guid id)
    {
        var response = await _http.DeleteAsync($"api/employees/{id}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<Department>> GetDepartmentsAsync(string? q = null) =>
        await _http.GetFromJsonAsync<List<Department>>("api/departments" + BuildQuery(new Dictionary<string, string?>
        {
            ["q"] = q
        })) ?? [];

    public async Task<List<NfcCard>> GetCardsAsync(string? q = null, string? cardType = null, bool? isActive = null, Guid? employeeId = null) =>
        await _http.GetFromJsonAsync<List<NfcCard>>("api/nfc-cards" + BuildQuery(new Dictionary<string, string?>
        {
            ["q"] = q,
            ["cardType"] = cardType,
            ["isActive"] = isActive?.ToString(),
            ["employeeId"] = employeeId?.ToString()
        })) ?? [];

    public async Task<NfcCard?> GetCardAsync(Guid id) =>
        await _http.GetFromJsonAsync<NfcCard>($"api/nfc-cards/{id}");

    public async Task CreateCardAsync(NfcCard card)
    {
        var response = await _http.PostAsJsonAsync("api/nfc-cards", card);
        response.EnsureSuccessStatusCode();
    }

    public async Task UpdateCardAsync(NfcCard card)
    {
        var response = await _http.PutAsJsonAsync($"api/nfc-cards/{card.Id}", card);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteCardAsync(Guid id)
    {
        var response = await _http.DeleteAsync($"api/nfc-cards/{id}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<AccessPoint>> GetAccessPointsAsync(string? q = null, bool? isActive = null) =>
        await _http.GetFromJsonAsync<List<AccessPoint>>("api/access-points" + BuildQuery(new Dictionary<string, string?>
        {
            ["q"] = q,
            ["isActive"] = isActive?.ToString()
        })) ?? [];

    public async Task<AccessPoint?> GetAccessPointAsync(Guid id) =>
        await _http.GetFromJsonAsync<AccessPoint>($"api/access-points/{id}");

    public async Task CreateAccessPointAsync(AccessPointUpsertRequest point)
    {
        var response = await _http.PostAsJsonAsync("api/access-points", point);
        response.EnsureSuccessStatusCode();
    }

    public async Task UpdateAccessPointAsync(Guid id, AccessPointUpsertRequest point)
    {
        var response = await _http.PutAsJsonAsync($"api/access-points/{id}", point);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteAccessPointAsync(Guid id)
    {
        var response = await _http.DeleteAsync($"api/access-points/{id}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<Schedule>> GetSchedulesAsync(string? q = null) =>
        await _http.GetFromJsonAsync<List<Schedule>>("api/schedules" + BuildQuery(new Dictionary<string, string?>
        {
            ["q"] = q
        })) ?? [];

    public async Task<Schedule?> GetScheduleAsync(Guid id) =>
        await _http.GetFromJsonAsync<Schedule>($"api/schedules/{id}");

    public async Task CreateScheduleAsync(Schedule schedule)
    {
        var response = await _http.PostAsJsonAsync("api/schedules", schedule);
        response.EnsureSuccessStatusCode();
    }

    public async Task UpdateScheduleAsync(Schedule schedule)
    {
        var response = await _http.PutAsJsonAsync($"api/schedules/{schedule.Id}", schedule);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteScheduleAsync(Guid id)
    {
        var response = await _http.DeleteAsync($"api/schedules/{id}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<AccessRule>> GetAccessRulesAsync(Guid? employeeId = null, Guid? accessPointId = null, Guid? scheduleId = null, bool? isActive = null) =>
        await _http.GetFromJsonAsync<List<AccessRule>>("api/access-rules" + BuildQuery(new Dictionary<string, string?>
        {
            ["employeeId"] = employeeId?.ToString(),
            ["accessPointId"] = accessPointId?.ToString(),
            ["scheduleId"] = scheduleId?.ToString(),
            ["isActive"] = isActive?.ToString()
        })) ?? [];

    public async Task<AccessRule?> GetAccessRuleAsync(Guid id) =>
        await _http.GetFromJsonAsync<AccessRule>($"api/access-rules/{id}");

    public async Task CreateAccessRuleAsync(AccessRule rule)
    {
        var response = await _http.PostAsJsonAsync("api/access-rules", rule);
        response.EnsureSuccessStatusCode();
    }

    public async Task UpdateAccessRuleAsync(AccessRule rule)
    {
        var response = await _http.PutAsJsonAsync($"api/access-rules/{rule.Id}", rule);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteAccessRuleAsync(Guid id)
    {
        var response = await _http.DeleteAsync($"api/access-rules/{id}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<AccessEvent>> GetAccessEventsAsync(int take = 200, string? cardUid = null, Guid? employeeId = null, Guid? accessPointId = null, bool? granted = null, string? reason = null, DateTime? fromUtc = null, DateTime? toUtc = null)
    {
        var query = BuildQuery(new Dictionary<string, string?>
        {
            ["take"] = take.ToString(),
            ["cardUid"] = cardUid,
            ["employeeId"] = employeeId?.ToString(),
            ["accessPointId"] = accessPointId?.ToString(),
            ["granted"] = granted?.ToString(),
            ["reason"] = reason,
            ["fromUtc"] = fromUtc?.ToString("o"),
            ["toUtc"] = toUtc?.ToString("o")
        });

        return await _http.GetFromJsonAsync<List<AccessEvent>>("api/access-events" + query) ?? [];
    }

    public async Task<ExportResult> ExportAccessEventsAsync(string format, IEnumerable<string> columns, DateTime? fromUtc, DateTime? toUtc)
    {
        var query = new List<string> { $"format={Uri.EscapeDataString(format)}" };
        foreach (var col in columns)
        {
            query.Add($"columns={Uri.EscapeDataString(col)}");
        }
        if (fromUtc.HasValue) query.Add($"fromUtc={Uri.EscapeDataString(fromUtc.Value.ToString("o"))}");
        if (toUtc.HasValue) query.Add($"toUtc={Uri.EscapeDataString(toUtc.Value.ToString("o"))}");

        var url = "api/access-events/export?" + string.Join("&", query);
        using var response = await _http.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var bytes = await response.Content.ReadAsByteArrayAsync();
        var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
        var fileName = response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
            ?? (format.Equals("xlsx", StringComparison.OrdinalIgnoreCase) ? "access_events.xlsx" : "access_events.csv");

        return new ExportResult(bytes, contentType, fileName);
    }

    public async Task<List<DeviceResponse>> GetDevicesAsync(string? q = null, bool? isActive = null) =>
        await _http.GetFromJsonAsync<List<DeviceResponse>>("api/devices" + BuildQuery(new Dictionary<string, string?>
        {
            ["q"] = q,
            ["isActive"] = isActive?.ToString()
        })) ?? [];

    public async Task<DeviceResponse?> GetDeviceAsync(Guid id) =>
        await _http.GetFromJsonAsync<DeviceResponse>($"api/devices/{id}");

    public async Task<DeviceWithTokenResponse> CreateDeviceAsync(DeviceUpsertRequest device)
    {
        var response = await _http.PostAsJsonAsync("api/devices", device);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DeviceWithTokenResponse>()
            ?? throw new InvalidOperationException("Device creation response was empty.");
    }

    public async Task UpdateDeviceAsync(Guid id, DeviceUpsertRequest device)
    {
        var response = await _http.PutAsJsonAsync($"api/devices/{id}", device);
        response.EnsureSuccessStatusCode();
    }

    public async Task<DeviceTokenResponse> RotateDeviceTokenAsync(Guid id)
    {
        var response = await _http.PostAsync($"api/devices/{id}/rotate-token", null);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DeviceTokenResponse>()
            ?? throw new InvalidOperationException("Device token rotation response was empty.");
    }

    public async Task DeleteDeviceAsync(Guid id)
    {
        var response = await _http.DeleteAsync($"api/devices/{id}");
        response.EnsureSuccessStatusCode();
    }

    private static string BuildQuery(Dictionary<string, string?> values)
    {
        var parts = values
            .Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
            .Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value!)}")
            .ToList();

        return parts.Count == 0 ? string.Empty : "?" + string.Join("&", parts);
    }

    public sealed record ExportResult(byte[] Bytes, string ContentType, string FileName);

    public sealed class EmployeeUpsertRequest
    {
        public string FullName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public byte[]? FaceImage { get; set; }
        public string? DepartmentNamesInput { get; set; }
    }

    public sealed class AccessPointUpsertRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Location { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsGuestAccess { get; set; }
        public List<Guid> SelectedEmployeeIds { get; set; } = new();
        public List<Guid> SelectedDepartmentIds { get; set; } = new();
    }

    public sealed class DeviceUpsertRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Location { get; set; }
        public Guid? AccessPointId { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public sealed class DeviceResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Location { get; set; }
        public Guid? AccessPointId { get; set; }
        public string? AccessPointName { get; set; }
        public string? TokenHint { get; set; }
        public DateTime? TokenLastRotatedAt { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public sealed class DeviceWithTokenResponse
    {
        public DeviceResponse Device { get; set; } = new();
        public string Token { get; set; } = string.Empty;
    }

    public sealed class DeviceTokenResponse
    {
        public Guid DeviceId { get; set; }
        public string DeviceName { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string TokenHint { get; set; } = string.Empty;
        public DateTime RotatedAtUtc { get; set; }
    }
}
