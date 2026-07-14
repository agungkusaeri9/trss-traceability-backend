using TraceabilitySystem.Application.DTOs.StockInRework;
using TraceabilitySystem.Shared.Models;

namespace TraceabilitySystem.Application.Interfaces;

public interface IStockInReworkService
{
    Task<PagedResult<StockInReworkDto>> GetPagedAsync(
        int page,
        int pageSize,
        FilterStockInReworkDto filter,
        CancellationToken cancellationToken = default);

    Task<StockInReworkDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IEnumerable<StockInReworkDto>> CreateAsync(CreateStockInReworkDto dto, CancellationToken cancellationToken = default);

    Task<StockInReworkDto> UpdateDispositionAsync(int id, UpdateStockInReworkDto dto, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
