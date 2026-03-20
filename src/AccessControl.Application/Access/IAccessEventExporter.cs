using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AccessControl.Application.Access;

public interface IAccessEventExporter
{
    Task<byte[]> ExportCsvAsync(AccessEventExportOptions options, CancellationToken cancellationToken = default);
    Task<byte[]> ExportXlsxAsync(AccessEventExportOptions options, CancellationToken cancellationToken = default);
}

public record AccessEventExportOptions(
    DateTime? FromUtc,
    DateTime? ToUtc,
    IReadOnlyList<string> Columns);
