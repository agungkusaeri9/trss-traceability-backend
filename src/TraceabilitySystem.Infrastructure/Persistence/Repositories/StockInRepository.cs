using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using TraceabilitySystem.Domain.Entities;
using TraceabilitySystem.Domain.Interfaces;

namespace TraceabilitySystem.Infrastructure.Persistence.Repositories;

public class StockInRepository : BaseRepository<StockIn>, IStockInRepository
{
    public StockInRepository(AppDbContext context) : base(context) { }

    public override async Task<StockIn?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(s => s.Part)
            .Include(s => s.Issues)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public override async Task<(IEnumerable<StockIn> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        Expression<Func<StockIn, bool>>? predicate = null,
        Func<IQueryable<StockIn>, IOrderedQueryable<StockIn>>? orderBy = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<StockIn> query = _dbSet
            .Include(s => s.Part)
            .Include(s => s.Issues);

        if (predicate is not null)
            query = query.Where(predicate);

        var totalCount = await query.CountAsync(cancellationToken);

        if (orderBy is not null)
            query = orderBy(query);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
