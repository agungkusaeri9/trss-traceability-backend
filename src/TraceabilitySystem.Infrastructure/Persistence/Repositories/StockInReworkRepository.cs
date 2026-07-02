using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using TraceabilitySystem.Domain.Entities;
using TraceabilitySystem.Domain.Interfaces;

namespace TraceabilitySystem.Infrastructure.Persistence.Repositories;

public class StockInReworkRepository : BaseRepository<StockInRework>, IStockInReworkRepository
{
    public StockInReworkRepository(AppDbContext context) : base(context) { }

    public override async Task<StockInRework?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(x => x.SerialNumber)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public override async Task<(IEnumerable<StockInRework> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        Expression<Func<StockInRework, bool>>? predicate = null,
        Func<IQueryable<StockInRework>, IOrderedQueryable<StockInRework>>? orderBy = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<StockInRework> query = _dbSet
            .Include(x => x.SerialNumber);

        if (predicate is not null)
            query = query.Where(predicate);

        var totalCount = await query.CountAsync(cancellationToken);

        query = query.OrderByDescending(x => x.CreatedAt);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
