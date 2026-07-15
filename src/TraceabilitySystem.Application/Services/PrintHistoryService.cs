
using Mapster;
using Microsoft.Extensions.Logging;
using TraceabilitySystem.Application.DTOs.PrintHistory;
using TraceabilitySystem.Application.DTOs.SerialNumber;
using TraceabilitySystem.Application.DTOs.StockIn;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Domain.Entities;
using TraceabilitySystem.Domain.Enums;
using TraceabilitySystem.Domain.Interfaces;
using TraceabilitySystem.Shared.Models;

namespace TraceabilitySystem.Application.Services
{
    public class PrintHistoryService : IPrintHistoryService
    {
        public readonly IPrintHistoryRepository _printHistoryRepository;
        private readonly ILogger<PrintHistory> _logger;
        private readonly IAppConfigRepository _appConfig;
        private readonly IStockInRepository _stockInRepository;
        private readonly ISerialNumberRepository _serialNumberRepo;
        public PrintHistoryService(IPrintHistoryRepository printHistoryRepository, ILogger<PrintHistory> logger, IAppConfigRepository appConfig, IStockInRepository stockInRepository, ISerialNumberRepository serialNumberRepo)
        {
            _printHistoryRepository = printHistoryRepository;
            _logger = logger;
            _appConfig = appConfig;
            _stockInRepository = stockInRepository;
            _serialNumberRepo = serialNumberRepo;
        }
        public async Task<PagedResult<PrintHistoryDto>> GetAllAsync(
       int page, int pageSize, string? searchTerm = null, CancellationToken cancellationToken = default)
        {
            var (processes, totalCount) = await _printHistoryRepository.GetPagedAsync(
                page,
                pageSize,
                //predicate: predicate,
                orderBy: q => q.OrderByDescending(u => u.CreatedAt),
                cancellationToken: cancellationToken);

            return new PagedResult<PrintHistoryDto>
            {
                Items = processes.Adapt<IEnumerable<PrintHistoryDto>>(),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<PrintHistoryDto> CreateAsync(PrintHistoryCreateDto dto)
        {
            var entity = dto.Adapt<PrintHistory>();

            await _printHistoryRepository.AddAsync(entity);
            await _printHistoryRepository.SaveChangesAsync();

            return entity.Adapt<PrintHistoryDto>(); 
            
        }

        public async Task CreateHistoryPrintClinchingAsync(PrintHistoryCreateClinchingDto request, CancellationToken cancellation = default)
        {
            var dto = new PrintHistoryCreateDto
            {
                Status = request.Status,
                Module = PrintModule.Clinching,
                ReferenceNumber = request.SerialNumberCode,
                ErrorMessage = request.ErrorMessage,
                PrinterName = await _appConfig.GetPrinterNameClinching(cancellation),
                RetryCount = 1,
                CreatedAt = DateTime.UtcNow
            };
            await CreateAsync(dto);

        }

        public async Task CreateHistoryPrintStockInAsync(PrintHistoryCreateStockInDto request, CancellationToken cancellation = default)
        {
            var dto = new PrintHistoryCreateDto
            {
                Status = request.Status,
                Module = PrintModule.StockIn,
                ReferenceNumber = request.IssueNumber,
                ErrorMessage = request.ErrorMessage,
                PrinterName = await _appConfig.GetPrinterNameStockIn(cancellation),
                RetryCount = 1,
                CreatedAt = DateTime.UtcNow
            };
            await CreateAsync(dto);

        }

        //private async Task RePrintStockInAsync(string issueNumber)
        //{
        //    var stockIn = await _stockInRepository.GetByIssueNumberAsync(issueNumber);
        //    if (stockIn is null)
        //        throw new KeyNotFoundException("Stock In not found.");
        //    var stockInDto = stockIn.Adapt<StockInDto>();
        //    await _printService.PrintStockInAsync(stockInDto);
        //}

        //private async Task RePrintClinchingAsync(string serialNumberCode, CancellationToken cancellationToken = default)
        //{
        //    var serialNumber = await _serialNumberRepo.GetWithRelatedBySerialNumberAsync(serialNumberCode);
        //    if (serialNumber is null)
        //        throw new KeyNotFoundException("Serial Number not found.");
        //    await _printService.PrintClinchingShortSideAsync(serialNumber.SerialNumberCode, cancellationToken);
        //}






    }
}
