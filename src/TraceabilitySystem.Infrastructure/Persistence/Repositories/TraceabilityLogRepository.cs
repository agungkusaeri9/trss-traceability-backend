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
            query = query.Where(x => (x.SerialNumber.SerialNumberCode.StartsWith("PVRA") || 
                                      x.SerialNumber.SerialNumberCode.StartsWith("CC") || 
                                      x.SerialNumber.Type == "CLINCHING") &&
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
            .Where(x => (x.SerialNumber.SerialNumberCode.StartsWith("PVRA") || x.SerialNumber.SerialNumberCode.StartsWith("CC") || x.SerialNumber.Type == "CLINCHING") &&
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
            .Where(x => (x.SerialNumber.SerialNumberCode.StartsWith("PVRA") || x.SerialNumber.SerialNumberCode.StartsWith("CC") || x.SerialNumber.Type == "CLINCHING") &&
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

    public async Task<TraceabilityLog> CreateNewAsync(
        TraceabilityLog traceabilityLog,
        CancellationToken cancellationToken = default)
    {
        if (traceabilityLog.Id == 0)
        {
            await _context.TraceabilityLogs.AddAsync(traceabilityLog, cancellationToken);
        }
        else
        {
            _context.TraceabilityLogs.Update(traceabilityLog);
        }
        await _context.SaveChangesAsync(cancellationToken);
        return traceabilityLog;
    }

    public async Task<TraceabilityLog?> GetTraceabilityLogByCodeAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        return await _context.TraceabilityLogs
            .Include(x => x.Details)
            .FirstOrDefaultAsync(x => x.Code == code, cancellationToken);
    }

    public async Task<(IEnumerable<TraceabilityLog> Items, int TotalCount)> GetAllTraceabilityNewAsync(
        int page,
        int pageSize,
        string? search = null,
        bool? status = null,
        bool? isFinish = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.TraceabilityLogs
            .AsNoTracking()
            .Include(x => x.Details)
                .ThenInclude(d => d.Process)
            .Include(x => x.Details)
                .ThenInclude(d => d.Parameter)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(x => x.Code.Contains(s) ||
                                     (x.SerialNumberClinching != null && x.SerialNumberClinching.Contains(s)) ||
                                     (x.SerialNumberMFan != null && x.SerialNumberMFan.Contains(s)));
        }

        if (status.HasValue)
        {
            query = query.Where(x => x.Status == status.Value);
        }

        if (isFinish.HasValue)
        {
            query = query.Where(x => x.IsFinish == isFinish.Value);
        }

        if (startDate.HasValue)
        {
            query = query.Where(x => x.CreatedAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(x => x.CreatedAt <= endDate.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<TraceabilityLog?> GetBySerialNumberClinchingAsync(
        string serialNumberClinching,
        CancellationToken cancellationToken = default)
    {
        return await _context.TraceabilityLogs
            .AsNoTracking()
            .Include(x => x.Details)
                .ThenInclude(d => d.Process)
            .Include(x => x.Details)
                .ThenInclude(d => d.Parameter)
            .FirstOrDefaultAsync(x => x.SerialNumberClinching == serialNumberClinching || x.Code == serialNumberClinching, cancellationToken);
    }

    public async Task<Dictionary<string, List<string>>> GetIssueNumbersBySerialNumbersAsync(
        IEnumerable<string> serialNumberCodes,
        CancellationToken cancellationToken = default)
    {
        var codes = serialNumberCodes
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct()
            .ToList();

        if (codes.Count == 0) return new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        var query = await _context.SerialNumbers
            .AsNoTracking()
            .Where(s => codes.Contains(s.SerialNumberCode))
            .Include(s => s.Issues)
                .ThenInclude(si => si.Issue)
                    .ThenInclude(i => i.StockIn)
                        .ThenInclude(st => st.Part)
            .ToListAsync(cancellationToken);

        var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in query)
        {
            var isClinching = string.Equals(s.Type, "CLINCHING", StringComparison.OrdinalIgnoreCase) ||
                              s.SerialNumberCode.StartsWith("PVRA", StringComparison.OrdinalIgnoreCase) ||
                              s.SerialNumberCode.StartsWith("CC", StringComparison.OrdinalIgnoreCase);

            var issues = s.Issues?
                .Where(si => si.Issue != null)
                .OrderBy(si => si.Id)
                .Select(si => new
                {
                    Number = si.Issue!.Number,
                    PartName = si.Issue.StockIn?.Part?.Name ?? string.Empty
                })
                .ToList() ?? new();

            List<string> issueNumbers;
            if (isClinching)
            {
                var clinchingIssues = issues
                    .Where(i => i.PartName.Contains("Core", StringComparison.OrdinalIgnoreCase) ||
                                i.PartName.Contains("Upper", StringComparison.OrdinalIgnoreCase) ||
                                i.PartName.Contains("Lower", StringComparison.OrdinalIgnoreCase) ||
                                i.PartName.Contains("Tank", StringComparison.OrdinalIgnoreCase))
                    .Select(i => i.Number)
                    .Distinct()
                    .ToList();

                issueNumbers = clinchingIssues.Count > 0
                    ? clinchingIssues
                    : issues.Select(i => i.Number).Distinct().ToList();
            }
            else
            {
                var mfanIssues = issues
                    .Where(i => i.PartName.Contains("Fan", StringComparison.OrdinalIgnoreCase) ||
                                i.PartName.Contains("Motor", StringComparison.OrdinalIgnoreCase) ||
                                i.PartName.Contains("Guide", StringComparison.OrdinalIgnoreCase))
                    .Select(i => i.Number)
                    .Distinct()
                    .ToList();

                issueNumbers = mfanIssues.Count > 0
                    ? mfanIssues
                    : issues.Select(i => i.Number).Distinct().ToList();
            }

            result[s.SerialNumberCode] = issueNumbers;
        }

        return result;
    }

    public async Task<IEnumerable<TraceabilityLog>> GetRecentTraceabilityLogsAsync(
        int count = 10,
        CancellationToken cancellationToken = default)
    {
        return await _context.TraceabilityLogs
            .AsNoTracking()
            .Include(x => x.Details)
                .ThenInclude(d => d.Process)
            .Include(x => x.Details)
                .ThenInclude(d => d.Parameter)
            .OrderByDescending(x => x.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<(string IssueNumber, string? PartNumber, string? PartName)>> GetIssuesBySerialNumberAsync(
        string serialNumber,
        bool status = false,
        bool isFinish = true,
        CancellationToken cancellationToken = default)
    {
        // Cari TraceabilityLog berdasarkan SerialNumberClinching dengan filter status & isFinish
        var traceLog = await _context.TraceabilityLogs
            .AsNoTracking()
            .Where(t => (t.SerialNumberClinching == serialNumber || t.Code == serialNumber)
                        && t.Status == status
                        && t.IsFinish == isFinish)
            .FirstOrDefaultAsync(cancellationToken);

        if (traceLog == null)
            return new List<(string, string?, string?)>();

        // Ambil serial number yang terkait (clinching)
        var snCode = traceLog.SerialNumberClinching ?? traceLog.Code;
        if (string.IsNullOrWhiteSpace(snCode))
            return new List<(string, string?, string?)>();

        var serial = await _context.SerialNumbers
            .AsNoTracking()
            .Where(s => s.SerialNumberCode == snCode)
            .Include(s => s.Issues)
                .ThenInclude(si => si.Issue)
                    .ThenInclude(i => i.StockIn)
                        .ThenInclude(st => st.Part)
            .FirstOrDefaultAsync(cancellationToken);

        if (serial == null)
            return new List<(string, string?, string?)>();

        return serial.Issues?
            .Where(si => si.Issue != null)
            .OrderBy(si => si.Id)
            .Select(si => (
                IssueNumber: si.Issue!.Number,
                PartNumber: si.Issue.StockIn?.Part?.Number,
                PartName: si.Issue.StockIn?.Part?.Name
            ))
            .ToList() ?? new List<(string, string?, string?)>();
    }
}



