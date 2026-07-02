using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using TraceabilitySystem.Domain.Entities;
using TraceabilitySystem.Domain.Interfaces;

namespace TraceabilitySystem.Infrastructure.Persistence.Repositories;

public class IssueRepository : BaseRepository<Issue>, IIssueRepository
{
    public IssueRepository(AppDbContext context) : base(context) { }

    /// <summary>
    /// Override untuk memastikan StockIn selalu di-include
    /// sehingga IssueService bisa membaca ReceiptQty tanpa query tambahan.
    /// </summary>
    public override async Task<Issue?> FirstOrDefaultAsync(
        Expression<Func<Issue, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(i => i.StockIn)
            .FirstOrDefaultAsync(predicate, cancellationToken);
    }

    public async Task<Dictionary<string, bool>> CheckIssueNumbersAsync(
    IEnumerable<string> issueNumbers,
    CancellationToken cancellationToken = default)
    {
        var issueNumberList = issueNumbers.Distinct().ToList();

        var existingIssueNumbers = await _context.Issues
            .Where(x => issueNumberList.Contains(x.Number))
            .Select(x => x.Number)
            .ToListAsync(cancellationToken);

        var existingSet = existingIssueNumbers.ToHashSet();

        return issueNumberList.ToDictionary(
            issueNumber => issueNumber,
            issueNumber => existingSet.Contains(issueNumber));
    }
}
