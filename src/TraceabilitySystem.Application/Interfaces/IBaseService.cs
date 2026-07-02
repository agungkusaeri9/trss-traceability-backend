using TraceabilitySystem.Shared.Models;

namespace TraceabilitySystem.Application.Interfaces;

public interface IBaseService<TEntity, TDto> where TEntity : class
{
    Task<PagedResult<TDto>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<TDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
