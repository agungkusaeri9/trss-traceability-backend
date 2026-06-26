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
        string? partNumber = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ProcessLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(serialNumberCode))
        {
            query = query.Include(x => x.SerialNumber)
                .Where(x => x.SerialNumber.SerialNumberCode.Contains(serialNumberCode));
        }

        if (isActive.HasValue)
        {
            query = query.Where(x => x.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(partNumber))
        {
            // Join with SerialNumber, SerialNumberIssue, Issue, StockIn, and Parts to filter by PartNumber
            query = from log in query
                    join serialNumber in _context.SerialNumbers on log.SerialNumberId equals serialNumber.Id
                    join snIssue in _context.SerialNumberIssues on serialNumber.Id equals snIssue.SerialNumberId
                    join issue in _context.Issues on snIssue.IssueId equals issue.Id
                    join stockIn in _context.StockIns on issue.StockInId equals stockIn.Id
                    join part in _context.Parts on stockIn.PartId equals part.Id
                    where part.Number.Contains(partNumber)
                    select log;
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Include(x => x.SerialNumber) // Include SerialNumber for easier access later
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<ProcessLog?> GetLogWithDetailsAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _context.ProcessLogs
            .Include(x => x.SerialNumber)
            .Include(x => x.Details)
                .ThenInclude(d => d.Process)
            .Include(x => x.Details)
                .ThenInclude(d => d.Parameter)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }
}
