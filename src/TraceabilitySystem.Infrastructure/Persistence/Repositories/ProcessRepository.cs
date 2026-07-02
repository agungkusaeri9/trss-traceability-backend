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

public class ProcessRepository : BaseRepository<Process>, IProcessRepository
{
    public ProcessRepository(AppDbContext context) : base(context) { }

    public override async Task<Process?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(p => p.ProcessParameters)
                .ThenInclude(pp => pp.Parameter)
            .FirstOrDefaultAsync(p => p.Id == (int)id, cancellationToken);
    }

    public override async Task<(IEnumerable<Process> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        Expression<Func<Process, bool>>? predicate = null,
        Func<IQueryable<Process>, IOrderedQueryable<Process>>? orderBy = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Process> query = _dbSet
            .Include(p => p.ProcessParameters)
                .ThenInclude(pp => pp.Parameter);

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

    public async Task<Dictionary<string, bool>> CheckParametersByProcessCodeAsync(
    string processCode,
    IEnumerable<string> parameterCodes,
    CancellationToken cancellationToken = default)
    {
        var parameterList = parameterCodes.Distinct().ToList();

        var existingParameters = await _dbSet
            .Where(p => p.Code == processCode)
            .SelectMany(p => p.ProcessParameters)
            .Select(pp => pp.Parameter.Code)
            .Where(pc => parameterList.Contains(pc))
            .ToListAsync(cancellationToken);

        var existingSet = existingParameters.ToHashSet();

        return parameterList.ToDictionary(
            parameterCode => parameterCode,
            parameterCode => existingSet.Contains(parameterCode));
    }
}
