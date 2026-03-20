using System.Globalization;
using AccessControl.Application.Access;
using AccessControl.Infrastructure.Data;
using CsvHelper;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;

namespace AccessControl.Infrastructure.Services;

public class AccessEventExporter : IAccessEventExporter
{
    private readonly AccessControlDbContext _db;

    public AccessEventExporter(AccessControlDbContext db)
    {
        _db = db;
    }

    public async Task<byte[]> ExportCsvAsync(AccessEventExportOptions options, CancellationToken cancellationToken = default)
    {
        var events = await LoadEventsAsync(options, cancellationToken);
        using var ms = new MemoryStream();
        using var writer = new StreamWriter(ms);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

        foreach (var column in options.Columns)
        {
            csv.WriteField(column);
        }
        await csv.NextRecordAsync();

        foreach (var evt in events)
        {
            foreach (var column in options.Columns)
            {
                csv.WriteField(GetValue(evt, column));
            }
            await csv.NextRecordAsync();
        }

        await writer.FlushAsync();
        return ms.ToArray();
    }

    public async Task<byte[]> ExportXlsxAsync(AccessEventExportOptions options, CancellationToken cancellationToken = default)
    {
        var events = await LoadEventsAsync(options, cancellationToken);
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("access_events");

        for (var i = 0; i < options.Columns.Count; i++)
        {
            sheet.Cell(1, i + 1).Value = options.Columns[i];
        }

        var row = 2;
        foreach (var evt in events)
        {
            for (var col = 0; col < options.Columns.Count; col++)
            {
                sheet.Cell(row, col + 1).Value = GetValue(evt, options.Columns[col]);
            }
            row++;
        }

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    private async Task<List<AccessEventRow>> LoadEventsAsync(AccessEventExportOptions options, CancellationToken cancellationToken)
    {
        var query = _db.AccessEvents
            .Include(e => e.Employee)
            .Include(e => e.AccessPoint)
            .Include(e => e.Device)
            .AsNoTracking();

        if (options.FromUtc.HasValue)
        {
            query = query.Where(e => e.EventTime >= options.FromUtc.Value);
        }

        if (options.ToUtc.HasValue)
        {
            query = query.Where(e => e.EventTime <= options.ToUtc.Value);
        }

        var events = await query
            .OrderByDescending(e => e.EventTime)
            .Select(e => new AccessEventRow(
                e.EventTime,
                e.CardUid,
                e.EmployeeId,
                e.Employee != null ? e.Employee.FullName : null,
                e.AccessPoint != null ? e.AccessPoint.Name : null,
                e.Device != null ? e.Device.Name : null,
                e.AccessGranted,
                e.Reason.ToString()))
            .ToListAsync(cancellationToken);

        return events;
    }

    private static string? GetValue(AccessEventRow row, string column)
    {
        return column switch
        {
            "event_time" => row.EventTime.ToString("u"),
            "card_uid" => row.CardUid,
            "employee_id" => row.EmployeeId?.ToString(),
            "employee_name" => row.EmployeeName,
            "access_point" => row.AccessPointName,
            "device" => row.DeviceName,
            "access_granted" => row.AccessGranted.ToString(),
            "reason" => row.Reason,
            _ => null
        };
    }

    private sealed record AccessEventRow(
        DateTime EventTime,
        string? CardUid,
        Guid? EmployeeId,
        string? EmployeeName,
        string? AccessPointName,
        string? DeviceName,
        bool AccessGranted,
        string Reason);
}
