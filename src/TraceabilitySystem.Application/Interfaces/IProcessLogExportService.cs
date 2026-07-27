using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using TraceabilitySystem.Application.DTOs.ProcessLog;
using TraceabilitySystem.Domain.Interfaces;

namespace TraceabilitySystem.Application.Interfaces;

public interface IProcessLogExportService
{
    Task<FileStreamResult> ExportToExcelAsync(
        string? serialNumberCode = null,
        bool? status = null,
        bool isFinished = true,
        CancellationToken cancellationToken = default);
}
