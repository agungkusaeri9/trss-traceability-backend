using Mapster;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Domain.Interfaces;
using TraceabilitySystem.Shared.Exceptions;
using TraceabilitySystem.Shared.Models;

namespace TraceabilitySystem.Application.Services;

public abstract class BaseService<TEntity, TDto> : IBaseService<TEntity, TDto> where TEntity : class
{
    protected readonly IRepository<TEntity> _repository;

    protected BaseService(IRepository<TEntity> repository)
    {
        _repository = repository;
    }

    public virtual async Task<PagedResult<TDto>> GetPagedAsync(
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(page, pageSize, cancellationToken: cancellationToken);

        return new PagedResult<TDto>
        {
            Items = items.Adapt<IEnumerable<TDto>>(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public virtual async Task<TDto> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(typeof(TEntity).Name, id);

        return entity.Adapt<TDto>();
    }

    public virtual async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(typeof(TEntity).Name, id);

        _repository.Remove(entity);
        await _repository.SaveChangesAsync(cancellationToken);
    }
}
