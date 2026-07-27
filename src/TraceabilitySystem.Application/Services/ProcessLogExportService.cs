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
using TraceabilitySystem.Application.Interfaces;

namespace TraceabilitySystem.Application.Services;

public class ProcessLogExportService : IProcessLogExportService
{
    private readonly IProcessLogRepository _processLogRepository;
    private readonly ISerialNumberRepository _serialNumberRepository;

    public ProcessLogExportService(
        IProcessLogRepository processLogRepository,
        ISerialNumberRepository serialNumberRepository)
    {
        _processLogRepository = processLogRepository;
        _serialNumberRepository = serialNumberRepository;
    }

    public async Task<FileStreamResult> ExportToExcelAsync(
        string? serialNumberCode = null,
        bool? status = null,
        bool isFinished = true,
        CancellationToken cancellationToken = default)
    {
        var logs = await _processLogRepository.GetAllAsync(cancellationToken);
        var ccLogs = logs.Where(x => x.SerialNumber != null && x.SerialNumber.SerialNumberCode.StartsWith("CC")).ToList();

        var dtos = new List<ExportProcessLogDto>();

        foreach (var log in ccLogs)
        {
            var serialNumber = await _serialNumberRepository.GetWithRelatedAsync(log.SerialNumberId, cancellationToken);
            if (serialNumber == null) continue;

            var issueNumbers = serialNumber.Issues?.Take(6).Select((x, i) => new { Index = i + 1, Number = x.Issue?.Number }).ToDictionary(x => x.Index, x => x.Number) ?? new Dictionary<int, string>();

            foreach (var detail in log.Details)
            {
                dtos.Add(new ExportProcessLogDto
                {
                    SerialNumberCode = serialNumber.SerialNumberCode,
                    IssueNumber1 = issueNumbers.TryGetValue(1, out var n1) ? n1 : null,
                    IssueNumber2 = issueNumbers.TryGetValue(2, out var n2) ? n2 : null,
                    IssueNumber3 = issueNumbers.TryGetValue(3, out var n3) ? n3 : null,
                    IssueNumber4 = issueNumbers.TryGetValue(4, out var n4) ? n4 : null,
                    IssueNumber5 = issueNumbers.TryGetValue(5, out var n5) ? n5 : null,
                    IssueNumber6 = issueNumbers.TryGetValue(6, out var n6) ? n6 : null,
                    ProcessName = detail.Process?.Name ?? string.Empty,
                    ParameterName = detail.Parameter?.Name ?? string.Empty,
                    Value = detail.DisplayValue,
                    Status = detail.Status,
                    CreatedAt = log.CreatedAt
                });
            }
        }

        var stream = new MemoryStream();
        using (var package = new ExcelPackage(stream))
        {
            var worksheet = package.Workbook.Worksheets.Add("Process Logs");

            worksheet.Cells[1, 1].Value = "Serial Number";
            worksheet.Cells[1, 2].Value = "Issue #1";
            worksheet.Cells[1, 3].Value = "Issue #2";
            worksheet.Cells[1, 4].Value = "Issue #3";
            worksheet.Cells[1, 5].Value = "Issue #4";
            worksheet.Cells[1, 6].Value = "Issue #5";
            worksheet.Cells[1, 7].Value = "Issue #6";
            worksheet.Cells[1, 8].Value = "Process";
            worksheet.Cells[1, 9].Value = "Parameter";
            worksheet.Cells[1, 10].Value = "Value";
            worksheet.Cells[1, 11].Value = "Status";
            worksheet.Cells[1, 12].Value = "Created At";

            for (int i = 0; i < dtos.Count; i++)
            {
                var dto = dtos[i];
                worksheet.Cells[i + 2, 1].Value = dto.SerialNumberCode;
                worksheet.Cells[i + 2, 2].Value = dto.IssueNumber1;
                worksheet.Cells[i + 2, 3].Value = dto.IssueNumber2;
                worksheet.Cells[i + 2, 4].Value = dto.IssueNumber3;
                worksheet.Cells[i + 2, 5].Value = dto.IssueNumber4;
                worksheet.Cells[i + 2, 6].Value = dto.IssueNumber5;
                worksheet.Cells[i + 2, 7].Value = dto.IssueNumber6;
                worksheet.Cells[i + 2, 8].Value = dto.ProcessName;
                worksheet.Cells[i + 2, 9].Value = dto.ParameterName;
                worksheet.Cells[i + 2, 10].Value = dto.Value;
                worksheet.Cells[i + 2, 11].Value = dto.Status;
                worksheet.Cells[i + 2, 12].Value = dto.CreatedAt;
            }

            worksheet.Cells[1, 1, 1, 12].Style.Font.Bold = true;
            worksheet.Cells[1, 1, dtos.Count + 1, 12].AutoFitColumns();

            package.Save();
        }

        stream.Position = 0;
        var fileName = $"process_logs_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

        return new FileStreamResult(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
        {
            FileDownloadName = fileName
        };
    }
}
