using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TraceabilitySystem.Domain.Entities;
using TraceabilitySystem.Domain.Interfaces;

namespace TraceabilitySystem.Infrastructure.Persistence.Repositories;

public class ProcessLogRepository : BaseRepository<ProcessLog>, IProcessLogRepository
{
    public ProcessLogRepository(AppDbContext context) : base(context) { }

    public async Task<(IEnumerable<ProcessLog> Items, int TotalCount)> GetPagedLogsAsync(
        int page,
        int pageSize,
        string? serialNumberCode = null,
        bool? status = null,
        bool? isFinished = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        bool clinchingOnly = false,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ProcessLogs
            .Include(x => x.SerialNumber)
            .AsQueryable();

        if (clinchingOnly)
        {
            query = query.Where(x => x.SerialNumber.SerialNumberCode.StartsWith("PVRA") || 
                                     x.SerialNumber.SerialNumberCode.StartsWith("CC") || 
                                     x.SerialNumber.Type == "CLINCHING");
        }

        if (isFinished.HasValue)
        {
            query = query.Where(x => x.IsFinished == isFinished.Value);
        }

        if (startDate.HasValue && endDate.HasValue)
        {
            var start = startDate.Value.Date;
            var end = endDate.Value.Date.AddDays(1);
            query = query.Where(x => x.CreatedAt >= start && x.CreatedAt < end);
        }
        else if (startDate.HasValue)
        {
            var start = startDate.Value.Date;
            var end = start.AddDays(1);
            query = query.Where(x => x.CreatedAt >= start && x.CreatedAt < end);
        }
        else if (endDate.HasValue)
        {
            var end = endDate.Value.Date.AddDays(1);
            query = query.Where(x => x.CreatedAt < end);
        }

        if (!string.IsNullOrWhiteSpace(serialNumberCode))
        {
            query = query.Where(x => x.SerialNumber.SerialNumberCode.Contains(serialNumberCode) ||
                                     x.SerialNumber.ParentRelations.Any(pr => pr.ChildSerialNumber.SerialNumberCode.Contains(serialNumberCode)) ||
                                     x.SerialNumber.ChildRelations.Any(cr => cr.ParentSerialNumber.SerialNumberCode.Contains(serialNumberCode)));
        }

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            // Parent SN issues
            .Include(x => x.SerialNumber)
                .ThenInclude(sn => sn.Issues)
                    .ThenInclude(sni => sni.Issue)
                        .ThenInclude(i => i.StockIn)
                            .ThenInclude(si => si.Part)
            // Child SN issues
            .Include(x => x.SerialNumber)
                .ThenInclude(sn => sn.ParentRelations)
                    .ThenInclude(r => r.ChildSerialNumber)
                        .ThenInclude(child => child.Issues)
                            .ThenInclude(sni => sni.Issue)
                                .ThenInclude(i => i.StockIn)
                                    .ThenInclude(si => si.Part)
            // Child SN process log details (process)
            .Include(x => x.SerialNumber)
                .ThenInclude(sn => sn.ParentRelations)
                    .ThenInclude(r => r.ChildSerialNumber)
                        .ThenInclude(child => child.ProcessLogs)
                            .ThenInclude(pl => pl.Details)
                                .ThenInclude(d => d.Process)
            // Child SN process log details (parameter)
            .Include(x => x.SerialNumber)
                .ThenInclude(sn => sn.ParentRelations)
                    .ThenInclude(r => r.ChildSerialNumber)
                        .ThenInclude(child => child.ProcessLogs)
                            .ThenInclude(pl => pl.Details)
                                .ThenInclude(d => d.Parameter)
            // Parent process log details
            .Include(x => x.Details)
                .ThenInclude(d => d.Process)
            .Include(x => x.Details)
                .ThenInclude(d => d.Parameter)
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<ProcessLog?> GetLogWithDetailsAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _context.ProcessLogs
            // Parent SN issues
            .Include(x => x.SerialNumber)
                .ThenInclude(sn => sn.Issues)
                    .ThenInclude(sni => sni.Issue)
                        .ThenInclude(i => i.StockIn)
                            .ThenInclude(si => si.Part)
            // Child SN issues
            .Include(x => x.SerialNumber)
                .ThenInclude(sn => sn.ParentRelations)
                    .ThenInclude(r => r.ChildSerialNumber)
                        .ThenInclude(child => child.Issues)
                            .ThenInclude(sni => sni.Issue)
                                .ThenInclude(i => i.StockIn)
                                    .ThenInclude(si => si.Part)
            // Child SN process log details (process)
            .Include(x => x.SerialNumber)
                .ThenInclude(sn => sn.ParentRelations)
                    .ThenInclude(r => r.ChildSerialNumber)
                        .ThenInclude(child => child.ProcessLogs)
                            .ThenInclude(pl => pl.Details)
                                .ThenInclude(d => d.Process)
            // Child SN process log details (parameter)
            .Include(x => x.SerialNumber)
                .ThenInclude(sn => sn.ParentRelations)
                    .ThenInclude(r => r.ChildSerialNumber)
                        .ThenInclude(child => child.ProcessLogs)
                            .ThenInclude(pl => pl.Details)
                                .ThenInclude(d => d.Parameter)
            // Parent process log details
            .Include(x => x.Details)
                .ThenInclude(d => d.Process)
            .Include(x => x.Details)
                .ThenInclude(d => d.Parameter)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }


 public async Task<ProcessLog?> GetLogBySerialNumberAsync(string serialNumber, CancellationToken cancellationToken = default)
    {
        return await _context.ProcessLogs
            // Parent SN issues
            .Include(x => x.SerialNumber)
                .ThenInclude(sn => sn.Issues)
                    .ThenInclude(sni => sni.Issue)
                        .ThenInclude(i => i.StockIn)
                            .ThenInclude(si => si.Part)
            // Child SN issues
            .Include(x => x.SerialNumber)
                .ThenInclude(sn => sn.ParentRelations)
                    .ThenInclude(r => r.ChildSerialNumber)
                        .ThenInclude(child => child.Issues)
                            .ThenInclude(sni => sni.Issue)
                                .ThenInclude(i => i.StockIn)
                                    .ThenInclude(si => si.Part)
            // Child SN process log details (process)
            .Include(x => x.SerialNumber)
                .ThenInclude(sn => sn.ParentRelations)
                    .ThenInclude(r => r.ChildSerialNumber)
                        .ThenInclude(child => child.ProcessLogs)
                            .ThenInclude(pl => pl.Details)
                                .ThenInclude(d => d.Process)
            // Child SN process log details (parameter)
            .Include(x => x.SerialNumber)
                .ThenInclude(sn => sn.ParentRelations)
                    .ThenInclude(r => r.ChildSerialNumber)
                        .ThenInclude(child => child.ProcessLogs)
                            .ThenInclude(pl => pl.Details)
                                .ThenInclude(d => d.Parameter)
            // Parent process log details
            .Include(x => x.Details)
                .ThenInclude(d => d.Process)
            .Include(x => x.Details)
                .ThenInclude(d => d.Parameter)
            .FirstOrDefaultAsync(x => x.SerialNumber.SerialNumberCode == serialNumber, cancellationToken);
    }

    public async Task<IEnumerable<ProcessLog>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ProcessLogs
            .Include(x => x.SerialNumber)
                .ThenInclude(sn => sn.ChildRelations)
                    .ThenInclude(cr => cr.ChildSerialNumber)
                        .ThenInclude(child => child.ProcessLogs)
                            .ThenInclude(pl => pl.Details)
                                .ThenInclude(d => d.Process)
            .Include(x => x.SerialNumber)
                .ThenInclude(sn => sn.ParentRelations)
                    .ThenInclude(pr => pr.ParentSerialNumber)
                        .ThenInclude(parent => parent.ProcessLogs)
                            .ThenInclude(pl => pl.Details)
                                .ThenInclude(d => d.Process)
            // Include both parent (PVRA/CC) and child (MF) serial numbers
            .Where(x => x.SerialNumber != null && 
                        (x.SerialNumber.SerialNumberCode.StartsWith("PVRA") || 
                         x.SerialNumber.SerialNumberCode.StartsWith("CC") || 
                         x.SerialNumber.SerialNumberCode.StartsWith("MF")))
            .Include(x => x.Details)
                .ThenInclude(d => d.Process)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountProductionAsync(DateTime startDate, DateTime endDate, bool? status, CancellationToken cancellationToken = default)
    {
        // Load clinching logs (PVRA/CC) with their details within date range.
        // "Finished" = log has a HE_LEAK detail (same definition as MapToListDto).
        var logs = await _context.ProcessLogs
            .Include(x => x.SerialNumber)
            .Include(x => x.Details)
                .ThenInclude(d => d.Process)
            .Include(x => x.Details)
                .ThenInclude(d => d.Parameter)
            .Where(x =>
                x.SerialNumber != null &&
                (x.SerialNumber.SerialNumberCode.StartsWith("PVRA") ||
                 x.SerialNumber.SerialNumberCode.StartsWith("CC") ||
                 x.SerialNumber.Type == "CLINCHING") &&
                x.CreatedAt >= startDate &&
                x.CreatedAt < endDate)
            .ToListAsync(cancellationToken);

        int count = 0;
        foreach (var log in logs)
        {
            var details = log.Details ?? new List<ProcessLogDetail>();

            // A clinching unit is considered "finished" when HE_LEAK has been recorded
            bool isFinished = log.IsFinished ||
                              details.Any(d => string.Equals(d.Process?.Code, "HE_LEAK", StringComparison.OrdinalIgnoreCase));

            if (!isFinished) continue;

            if (status == null)
            {
                // Count all finished units
                count++;
            }
            else
            {
                // Evaluate actual status from process detail parameters (same logic as MapToListDto)
                int? oRingSet = GetDetailInt(details, "CLINCHING_SHORT_SIDE", "O_RING_SET_RESULT", "O_RING_SET", "ORING_SET_RESULT");
                int? capTypePos = GetDetailInt(details, "HE_LEAK", "CAP_TYPE_POSITION_RESULT", "CAP_TYPE_POSITION", "CAP_TYPE");
                int? leakResult = GetDetailInt(details, "HE_LEAK", "LEAK_RESULT", "LEAK_TEST_RESULT", "LEAK_STATUS");

                var endPlateDetails = details
                    .Where(d => string.Equals(d.Process?.Code, "CLINCHING_LONG_SIDE", StringComparison.OrdinalIgnoreCase) &&
                                (d.Parameter?.Code?.StartsWith("END_PLATE_WIDTH", StringComparison.OrdinalIgnoreCase) == true))
                    .ToList();
                int[] endPlateResults = endPlateDetails.Select(d => GetDetailIntValue(d)).ToArray();
                bool? endPlateStatus = endPlateResults.Length > 0 ? endPlateResults.All(x => x == 1) : null;

                var clinchingHeightDetails = details
                    .Where(d => string.Equals(d.Process?.Code, "CLINCHING_LONG_SIDE", StringComparison.OrdinalIgnoreCase) &&
                                (d.Parameter?.Code?.StartsWith("CLINCHING_HEIGHT", StringComparison.OrdinalIgnoreCase) == true))
                    .ToList();
                double[] clinchingHeightValues = clinchingHeightDetails.Select(d => d.ValueNumber.HasValue ? (double)d.ValueNumber.Value : 0.0).ToArray();
                bool? clinchingHeightStatus = clinchingHeightValues.Length > 0 ? clinchingHeightValues.All(x => x == 1) : null;

                var clinchingAndHeDetails = details
                    .Where(d => string.Equals(d.Process?.Code, "CLINCHING_SHORT_SIDE", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(d.Process?.Code, "CLINCHING_LONG_SIDE", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(d.Process?.Code, "HE_LEAK", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                bool typeStatus =
                    (oRingSet == null || oRingSet == 1) &&
                    (clinchingHeightStatus == null || clinchingHeightStatus.Value) &&
                    (endPlateStatus == null || endPlateStatus.Value) &&
                    (capTypePos == null || capTypePos == 1) &&
                    (leakResult == null || leakResult == 1) &&
                    (clinchingAndHeDetails.Count == 0 || clinchingAndHeDetails.All(d => d.Status));

                if (typeStatus == status.Value)
                    count++;
            }
        }

        return count;
    }

    // Helper: find detail by process code + one of several parameter codes
    private static int? GetDetailInt(IEnumerable<ProcessLogDetail> details, string processCode, params string[] paramCodes)
    {
        var detail = details.FirstOrDefault(d =>
            string.Equals(d.Process?.Code, processCode, StringComparison.OrdinalIgnoreCase) &&
            paramCodes.Any(pc => string.Equals(d.Parameter?.Code, pc, StringComparison.OrdinalIgnoreCase)));

        if (detail == null) return null;
        if (detail.ValueNumber.HasValue) return (int)detail.ValueNumber.Value;
        if (detail.ValueBoolean.HasValue) return detail.ValueBoolean.Value ? 1 : 2;
        return detail.Status ? 1 : 2;
    }

    private static int GetDetailIntValue(ProcessLogDetail detail)
    {
        if (detail.ValueNumber.HasValue) return (int)detail.ValueNumber.Value;
        if (detail.ValueBoolean.HasValue) return detail.ValueBoolean.Value ? 1 : 2;
        return detail.Status ? 1 : 2;
    }

    public async Task<ProcessLog> AddProcessLogPerProcessAsync(
        string serialNumberCode,
        string processCode,
        bool isOk,
        List<(string parameterCode, decimal? valueNumber, string? valueText, bool? valueBoolean, bool status)> parameters,
        CancellationToken cancellationToken = default)
    {
        // 1. Get Serial Number by Code
        var serialNumber = await _context.SerialNumbers
            .FirstOrDefaultAsync(x => x.SerialNumberCode == serialNumberCode, cancellationToken);

        if (serialNumber == null)
            throw new KeyNotFoundException($"Serial Number with code '{serialNumberCode}' not found.");

        // 2. Get Process by Code
        var process = await _context.Processes
            .FirstOrDefaultAsync(x => x.Code == processCode, cancellationToken);

        if (process == null)
            throw new KeyNotFoundException($"Process with code '{processCode}' not found.");

        // 3. Check if there's already an active Process Log for this Serial Number
        var processLog = await _context.ProcessLogs
            .FirstOrDefaultAsync(x => x.SerialNumberId == serialNumber.Id && x.IsActive, cancellationToken);

        bool shouldFinish = !isOk || 
                            string.Equals(processCode?.Trim(), "HE_LEAK", StringComparison.OrdinalIgnoreCase) || 
                            string.Equals(processCode?.Trim(), "M_FAN_INSPECTION", StringComparison.OrdinalIgnoreCase) || 
                            string.Equals(processCode?.Trim(), "FINAL_INSPECTION", StringComparison.OrdinalIgnoreCase);

        // 4. Create or update Process Log
        if (processLog == null)
        {
            processLog = new ProcessLog
            {
                SerialNumberId = serialNumber.Id,
                IsActive = true,
                Status = isOk,
                IsFinished = shouldFinish,
                CreatedAt = DateTime.Now
            };
            await AddAsync(processLog, cancellationToken);
            await SaveChangesAsync(cancellationToken);
        }
        else
        {
            processLog.UpdatedAt = DateTime.Now;
            if (shouldFinish)
            {
                processLog.IsFinished = true;
            }
            if (!isOk)
            {
                processLog.Status = false;
            }
            Update(processLog);
            await SaveChangesAsync(cancellationToken);
        }

        // 5. Get Parameters by Codes
        var parameterCodes = parameters.Select(p => p.parameterCode).ToList();
        var existingParameters = await _context.Parameters
            .Where(x => parameterCodes.Contains(x.Code))
            .ToListAsync(cancellationToken);

        // 6. Add Process Log Details
        foreach (var param in parameters)
        {
            var parameter = existingParameters.FirstOrDefault(x => x.Code == param.parameterCode);
            if (parameter == null)
                continue; // Skip if parameter not found

            var detail = new ProcessLogDetail
            {
                ProcessLogId = processLog.Id,
                ProcessId = process.Id,
                ParameterId = parameter.Id,
                ValueNumber = param.valueNumber,
                ValueText = param.valueText,
                ValueBoolean = param.valueBoolean,
                Status = param.status,
                CreatedAt = DateTime.Now
            };
            await _context.ProcessLogDetails.AddAsync(detail, cancellationToken);
        }

        await SaveChangesAsync(cancellationToken);
        return processLog;
    }

    public async Task<ProcessLog?> GetProcessLogFullValueAsync(
    string serialNumberCode,
    CancellationToken cancellationToken = default)
    {
        return await _context.ProcessLogs
            .AsNoTracking()

            // Current Serial Number
            .Include(x => x.SerialNumber)

            // Current Process Details
            .Include(x => x.Details)
                .ThenInclude(d => d.Process)

            .Include(x => x.Details)
                .ThenInclude(d => d.Parameter)

            // Parent -> Child Relations
            .Include(x => x.SerialNumber)
                .ThenInclude(sn => sn.ParentRelations)
                    .ThenInclude(r => r.ChildSerialNumber)
                        .ThenInclude(sn => sn.ProcessLogs)
                            .ThenInclude(pl => pl.Details)
                                .ThenInclude(d => d.Process)

            .Include(x => x.SerialNumber)
                .ThenInclude(sn => sn.ParentRelations)
                    .ThenInclude(r => r.ChildSerialNumber)
                        .ThenInclude(sn => sn.ProcessLogs)
                            .ThenInclude(pl => pl.Details)
                                .ThenInclude(d => d.Parameter)

             .FirstOrDefaultAsync(
            x => x.SerialNumber.SerialNumberCode == serialNumberCode &&
                 (x.SerialNumber.SerialNumberCode.StartsWith("PVRA") || x.SerialNumber.SerialNumberCode.StartsWith("CC") || x.SerialNumber.Type == "CLINCHING"),
            cancellationToken);
    }
}
