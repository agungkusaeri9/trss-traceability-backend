using AutoMapper;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Domain.Interfaces;
using TraceabilitySystem.Shared.Exceptions;
using TraceabilitySystem.Shared.Models;

namespace TraceabilitySystem.Application.Services;

public abstract class BaseService<TEntity, TDto> : IBaseService<TEntity, TDto> where TEntity : class
{
    protected readonly IRepository<TEntity> _repository;
    protected readonly IMapper _mapper;

    protected BaseService(IRepository<TEntity> repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public virtual async Task<PagedResult<TDto>> GetPagedAsync(
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(page, pageSize, cancellationToken: cancellationToken);

        return new PagedResult<TDto>
        {
            Items = _mapper.Map<IEnumerable<TDto>>(items),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public virtual async Task<TDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(typeof(TEntity).Name, id);

        return _mapper.Map<TDto>(entity);
    }

    public virtual async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(typeof(TEntity).Name, id);

        _repository.Remove(entity);
        await _repository.SaveChangesAsync(cancellationToken);
    }
}
