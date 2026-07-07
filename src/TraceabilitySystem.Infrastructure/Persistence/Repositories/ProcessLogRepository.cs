using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TraceabilitySystem.Application.DTOs.Dashboard;
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
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ProcessLogs
            .Include(x => x.SerialNumber)
            .Where(x => x.SerialNumber.SerialNumberCode.StartsWith("CC"))
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(serialNumberCode))
        {
            query = query.Where(x => x.SerialNumber.SerialNumberCode.Contains(serialNumberCode));
        }

        if (isActive.HasValue)
        {
            query = query.Where(x => x.IsActive == isActive.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            // Parent SN issues
            .Include(x => x.SerialNumber)
                .ThenInclude(sn => sn.Issues)
                    .ThenInclude(sni => sni.Issue)
                        .ThenInclude(i => i.StockIn)
                            .ThenInclude(si => si!.Part)
            // Child SN issues
            .Include(x => x.SerialNumber)
                .ThenInclude(sn => sn.ParentRelations)
                    .ThenInclude(r => r.ChildSerialNumber)
                        .ThenInclude(child => child.Issues)
                            .ThenInclude(sni => sni.Issue)
                                .ThenInclude(i => i.StockIn)
                                    .ThenInclude(si => si!.Part)
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
                            .ThenInclude(si => si!.Part)
            // Child SN issues
            .Include(x => x.SerialNumber)
                .ThenInclude(sn => sn.ParentRelations)
                    .ThenInclude(r => r.ChildSerialNumber)
                        .ThenInclude(child => child.Issues)
                            .ThenInclude(sni => sni.Issue)
                                .ThenInclude(i => i.StockIn)
                                    .ThenInclude(si => si!.Part)
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
                            .ThenInclude(si => si!.Part)
            // Child SN issues
            .Include(x => x.SerialNumber)
                .ThenInclude(sn => sn.ParentRelations)
                    .ThenInclude(r => r.ChildSerialNumber)
                        .ThenInclude(child => child.Issues)
                            .ThenInclude(sni => sni.Issue)
                                .ThenInclude(i => i.StockIn)
                                    .ThenInclude(si => si!.Part)
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
            // Include both parent (CC) and child (MF) serial numbers
            .Where(x => x.SerialNumber != null && 
                        (x.SerialNumber.SerialNumberCode.StartsWith("CC") || 
                         x.SerialNumber.SerialNumberCode.StartsWith("MF")))
            .Include(x => x.Details)
                .ThenInclude(d => d.Process)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
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

        // 4. Create or update Process Log
        if (processLog == null)
        {
            processLog = new ProcessLog
            {
                SerialNumberId = serialNumber.Id,
                IsActive = true,
                Status = isOk,
                CreatedAt = DateTime.Now
            };
            await AddAsync(processLog, cancellationToken);
            await SaveChangesAsync(cancellationToken);
        }
        else
        {
            processLog.UpdatedAt = DateTime.Now;
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


    public async Task<int> GetTotalProductionAsync(DateTime? startDate, CancellationToken cancellationToken)
    {
        return await _context.ProcessLogs
            .Where(x => x.IsFinished &&
                        (!startDate.HasValue || x.CreatedAt >= startDate.Value))
            .CountAsync(cancellationToken);
    }

    public async Task<int> GetOkCountAsync(DateTime? startDate, CancellationToken cancellationToken)
    {
        return await _context.ProcessLogs
            .Where(x => x.IsFinished &&
                        x.Status &&
                        (!startDate.HasValue || x.CreatedAt >= startDate.Value))
            .CountAsync(cancellationToken);
    }

    public async Task<int> GetNgCountAsync(DateTime? startDate, CancellationToken cancellationToken)
    {
        return await _context.ProcessLogs
            .Where(x => x.IsFinished &&
                        x.Status == false &&
                        (!startDate.HasValue || x.CreatedAt >= startDate.Value))
            .CountAsync(cancellationToken);
    }

    public async Task<List<(string Label, int Value)>> GetProductionTrendAsync(
     int days = 7,
     CancellationToken cancellationToken = default)
    {
        var result = new List<(string Label, int Value)>();

        for (int i = days - 1; i >= 0; i--)
        {
            var date = DateTime.Today.AddDays(-i);
            var nextDate = date.AddDays(1);

            var count = await _dbSet.CountAsync(
                x => x.IsFinished &&
                     x.CreatedAt >= date &&
                     x.CreatedAt < nextDate,
                cancellationToken);

            result.Add((date.ToString("dd MMM"), count));
        }

        return result;
    }

    public async Task<List<(string Label, int Value)>> GetTopPartsProductionAsync(
    DateTime startDate,
    int take = 5,
    CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(x => x.IsFinished && x.CreatedAt >= startDate)
            .SelectMany(x => x.SerialNumber.Issues
                .Where(i => i.Issue != null &&
                            i.Issue.StockIn != null &&
                            i.Issue.StockIn.Part != null)
                .Select(i => i.Issue!.StockIn!.Part!.Number))
            .GroupBy(x => x)
            .Select(g => new
            {
                Label = g.Key,
                Value = g.Count()
            })
            .OrderByDescending(x => x.Value)
            .Take(take)
            .Select(x => ValueTuple.Create(x.Label, x.Value))
            .ToListAsync(cancellationToken);
    }
}
