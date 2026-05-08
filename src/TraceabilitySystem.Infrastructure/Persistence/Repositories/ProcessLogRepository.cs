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
        string? issueNo = null,
        string? partNumber = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ProcessLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(issueNo))
        {
            query = query.Where(x => x.IssueNo.Contains(issueNo));
        }

        if (isActive.HasValue)
        {
            query = query.Where(x => x.IsActive == isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(partNumber))
        {
            // Join with Issues, StockIn, and Parts to filter by PartNumber
            query = from log in query
                    join issue in _context.Issues on log.IssueNo equals issue.Number
                    join stockIn in _context.StockIns on issue.StockInId equals stockIn.Id
                    join part in _context.Parts on stockIn.PartId equals part.Id
                    where part.Number.Contains(partNumber)
                    select log;
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<ProcessLog?> GetLogWithDetailsAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _context.ProcessLogs
            .Include(x => x.Details)
                .ThenInclude(d => d.Process)
            .Include(x => x.Details)
                .ThenInclude(d => d.Parameter)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }
}
