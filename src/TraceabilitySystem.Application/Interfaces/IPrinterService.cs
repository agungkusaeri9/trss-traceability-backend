using System.Threading;
using System.Threading.Tasks;
using TraceabilitySystem.Application.DTOs.Pagination;
using TraceabilitySystem.Application.DTOs.Printer;
using TraceabilitySystem.Domain.Entities;
using TraceabilitySystem.Application.DTOs.StockIn;
using TraceabilitySystem.Shared.Models;

namespace TraceabilitySystem.Application.Interfaces;

public interface IPrinterService : IBaseService<Printer, PrinterDto>
{
    Task<PagedResult<PrinterDto>> GetPrintersAsync(
        int page,
        int pageSize,
        string? search = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default);

    Task<PrinterDto> GetPrinterByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<PrinterDto> CreatePrinterAsync(CreatePrinterRequestDto request, CancellationToken cancellationToken = default);

    Task<PrinterDto> UpdatePrinterAsync(int id, UpdatePrinterRequestDto request, CancellationToken cancellationToken = default);

    Task DeletePrinterAsync(int id, CancellationToken cancellationToken = default);

    Task PrintLabelStockIn(StockInDto stockIn);
}
