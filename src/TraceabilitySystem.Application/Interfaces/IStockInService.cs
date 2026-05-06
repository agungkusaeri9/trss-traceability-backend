using System;
using System.Threading;
using System.Threading.Tasks;
using TraceabilitySystem.Application.DTOs.StockIn;
using TraceabilitySystem.Domain.Entities;
using TraceabilitySystem.Shared.Models;

namespace TraceabilitySystem.Application.Interfaces;

public interface IStockInService : IBaseService<StockIn, StockInDto>
{
    Task<PagedResult<StockInDto>> GetStockInsAsync(
        int page,
        int pageSize,
        DateTime? date = null,
        string? issueNumber = null,
        string? partNumber = null,
        CancellationToken cancellationToken = default);

    Task<StockInDto> GetStockInByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<StockInDto> CreateStockInAsync(CreateStockInRequestDto request, CancellationToken cancellationToken = default);
}
