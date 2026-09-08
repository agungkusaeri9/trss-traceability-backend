using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TraceabilitySystem.Domain.Entities;
using TraceabilitySystem.Domain.Interfaces;

namespace TraceabilitySystem.Infrastructure.Persistence.Repositories;

public class TraceabilityLogRepository : BaseRepository<ProcessLog>, ITraceabilityLogRepository
{
    public TraceabilityLogRepository(AppDbContext context) : base(context) { }

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
            query = query.Where(x => (x.SerialNumber.SerialNumberCode.StartsWith("CC") || x.SerialNumber.Type == "CLINCHING") &&
                                     x.SerialNumber.ParentRelations.Any(pr => pr.ChildSerialNumber != null));
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
        // 1. Prioritize Clinching (Parent) ProcessLog matching serialNumber directly or via child relation
        var log = await _context.ProcessLogs
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
            .Where(x => (x.SerialNumber.SerialNumberCode.StartsWith("CC") || x.SerialNumber.Type == "CLINCHING") &&
                        (x.SerialNumber.SerialNumberCode == serialNumber ||
                         x.SerialNumber.ParentRelations.Any(pr => pr.ChildSerialNumber.SerialNumberCode == serialNumber)))
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (log != null) return log;

        // 2. Fallback: If not found as parent, search any log matching the serial number directly
        return await _context.ProcessLogs
            .Include(x => x.SerialNumber)
                .ThenInclude(sn => sn.Issues)
                    .ThenInclude(sni => sni.Issue)
                        .ThenInclude(i => i.StockIn)
                            .ThenInclude(si => si.Part)
            .Include(x => x.SerialNumber)
                .ThenInclude(sn => sn.ParentRelations)
                    .ThenInclude(r => r.ChildSerialNumber)
                        .ThenInclude(child => child.Issues)
                            .ThenInclude(sni => sni.Issue)
                                .ThenInclude(i => i.StockIn)
                                    .ThenInclude(si => si.Part)
            .Include(x => x.SerialNumber)
                .ThenInclude(sn => sn.ParentRelations)
                    .ThenInclude(r => r.ChildSerialNumber)
                        .ThenInclude(child => child.ProcessLogs)
                            .ThenInclude(pl => pl.Details)
                                .ThenInclude(d => d.Process)
            .Include(x => x.SerialNumber)
                .ThenInclude(sn => sn.ParentRelations)
                    .ThenInclude(r => r.ChildSerialNumber)
                        .ThenInclude(child => child.ProcessLogs)
                            .ThenInclude(pl => pl.Details)
                                .ThenInclude(d => d.Parameter)
            .Include(x => x.Details)
                .ThenInclude(d => d.Process)
            .Include(x => x.Details)
                .ThenInclude(d => d.Parameter)
            .Where(x => x.SerialNumber.SerialNumberCode == serialNumber)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ProcessLog?> GetProcessLogFullValueAsync(
        string serialNumberCode,
        CancellationToken cancellationToken = default)
    {
        // 1. Prioritize Clinching (Parent) log
        var log = await _context.ProcessLogs
            .AsNoTracking()
            // Parent SN issues
            .Include(x => x.SerialNumber)
                .ThenInclude(sn => sn.Issues)
                    .ThenInclude(sni => sni.Issue)
                        .ThenInclude(i => i.StockIn)
                            .ThenInclude(si => si.Part)
            // Current Process Details
            .Include(x => x.Details)
                .ThenInclude(d => d.Process)
            .Include(x => x.Details)
                .ThenInclude(d => d.Parameter)
            // Parent -> Child Relations with Issues & Logs
            .Include(x => x.SerialNumber)
                .ThenInclude(sn => sn.ParentRelations)
                    .ThenInclude(r => r.ChildSerialNumber)
                        .ThenInclude(child => child.Issues)
                            .ThenInclude(sni => sni.Issue)
                                .ThenInclude(i => i.StockIn)
                                    .ThenInclude(si => si.Part)
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
            .Where(x => (x.SerialNumber.SerialNumberCode.StartsWith("CC") || x.SerialNumber.Type == "CLINCHING") &&
                        (x.SerialNumber.SerialNumberCode == serialNumberCode ||
                         x.SerialNumber.ParentRelations.Any(pr => pr.ChildSerialNumber.SerialNumberCode == serialNumberCode)))
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (log != null) return log;

        // 2. Fallback
        return await _context.ProcessLogs
            .AsNoTracking()
            .Include(x => x.SerialNumber)
                .ThenInclude(sn => sn.Issues)
                    .ThenInclude(sni => sni.Issue)
                        .ThenInclude(i => i.StockIn)
                            .ThenInclude(si => si.Part)
            .Include(x => x.Details)
                .ThenInclude(d => d.Process)
            .Include(x => x.Details)
                .ThenInclude(d => d.Parameter)
            .Include(x => x.SerialNumber)
                .ThenInclude(sn => sn.ParentRelations)
                    .ThenInclude(r => r.ChildSerialNumber)
                        .ThenInclude(child => child.Issues)
                            .ThenInclude(sni => sni.Issue)
                                .ThenInclude(i => i.StockIn)
                                    .ThenInclude(si => si.Part)
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
            .Where(x => x.SerialNumber.SerialNumberCode == serialNumberCode)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
