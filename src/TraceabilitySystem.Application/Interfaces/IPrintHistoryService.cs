using System;
using System.Collections.Generic;
using System.Text;
using TraceabilitySystem.Application.DTOs.PrintHistory;
using TraceabilitySystem.Application.DTOs.Process;
using TraceabilitySystem.Shared.Models;

namespace TraceabilitySystem.Application.Interfaces
{
    public interface IPrintHistoryService
    {
        Task<PagedResult<PrintHistoryDto>> GetAllAsync(int page, int pageSize, string? searchTerm = null, CancellationToken cancellationToken = default);
        //Task<PrintHistoryDto> CreatePrintStockIn(CreateProcessRequestDto request, CancellationToken cancellationToken = default);
        Task<PrintHistoryDto> CreateAsync(PrintHistoryCreateDto request);
        Task CreateHistoryPrintClinchingAsync(PrintHistoryCreateClinchingDto request, CancellationToken cancellation = default);
        Task CreateHistoryPrintStockInAsync(PrintHistoryCreateStockInDto request, CancellationToken cancellation = default);

    }
}
