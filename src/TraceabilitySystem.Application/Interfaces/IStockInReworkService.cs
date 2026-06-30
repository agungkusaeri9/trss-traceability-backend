using TraceabilitySystem.Application.DTOs.StockInRework;
using TraceabilitySystem.Shared.Models;

namespace TraceabilitySystem.Application.Interfaces;

public interface IStockInReworkService
{
    Task<PagedResult<StockInReworkDto>> GetPagedAsync(
        int page,
        int pageSize,
        long? serialNumberId = null,
        CancellationToken cancellationToken = default);

    Task<StockInReworkDto> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<IEnumerable<StockInReworkDto>> CreateAsync(CreateStockInReworkDto dto, CancellationToken cancellationToken = default);

    Task<StockInReworkDto> UpdateAsync(long id, UpdateStockInReworkDto dto, CancellationToken cancellationToken = default);

    Task DeleteAsync(long id, CancellationToken cancellationToken = default);
}
