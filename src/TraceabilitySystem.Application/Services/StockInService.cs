using Mapster;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using TraceabilitySystem.Application.DTOs.StockIn;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Domain.Entities;
using TraceabilitySystem.Domain.Interfaces;
using TraceabilitySystem.Shared.Exceptions;
using TraceabilitySystem.Shared.Models;

namespace TraceabilitySystem.Application.Services;

public class StockInService : BaseService<StockIn, StockInDto>, IStockInService
{
    private readonly IPartRepository _partRepository;
    private readonly IPrinterService _printerService;
    private readonly IStockInRepository _stockInRepository;

    public StockInService(
        IStockInRepository repository,
        IPartRepository partRepository,
        IPrinterService printerService) : base(repository)
    {
        _stockInRepository = repository;
        _partRepository = partRepository;
        _printerService = printerService;
    }

    public async Task<PagedResult<StockInDto>> GetStockInsAsync(
        int page,
        int pageSize,
        DateTime? date = null,
        string? issueNumber = null,
        string? partNumber = null,
        CancellationToken cancellationToken = default)
    {
        Expression<Func<StockIn, bool>>? predicate = null;

        if (date.HasValue || !string.IsNullOrEmpty(issueNumber) || !string.IsNullOrEmpty(partNumber))
        {
            predicate = s =>
                (!date.HasValue || s.SupplyDate.Date == date.Value.Date) &&
                (string.IsNullOrEmpty(issueNumber) || s.Issues.Any(i => i.Number.Contains(issueNumber))) &&
                (string.IsNullOrEmpty(partNumber) || s.Part!.Number.Contains(partNumber));
        }

        var (items, totalCount) = await _stockInRepository.GetPagedAsync(
            page,
            pageSize,
            predicate,
            q => q.OrderByDescending(s => s.CreatedAt),
            cancellationToken);

        return new PagedResult<StockInDto>
        {
            Items = items.Adapt<System.Collections.Generic.IEnumerable<StockInDto>>(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<StockInDto> GetStockInByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _stockInRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(StockIn), id);

        return entity.Adapt<StockInDto>();
    }

    public async Task<StockInDto> CreateStockInAsync(CreateStockInRequestDto request, CancellationToken cancellationToken = default)
    {
        // 1. Validate Part exists
        var partExists = await _partRepository.ExistsAsync(p => p.Id == request.PartId, cancellationToken);
        if (!partExists)
            throw new NotFoundException(nameof(Part), request.PartId);

        // 2. Generate auto-generated codes based on the highest sequence today
        var today = DateTime.UtcNow.Date;
        var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
        
        // Fetch all codes starting with STyyyyMMdd to find the max sequence
        var prefix = $"ST{datePart}";
        var existingCodes = await _stockInRepository.FindAsync(s => s.Code.StartsWith(prefix), cancellationToken);
        
        int nextSeq = 1;
        if (existingCodes.Any())
        {
            var maxSeq = existingCodes
                .Select(s => s.Code.Replace(prefix, ""))
                .Select(s => int.TryParse(s, out int val) ? val : 0)
                .Max();
            nextSeq = maxSeq + 1;
        }

        var stockInCode = $"{prefix}{nextSeq:D3}";
        var issueNumber = $"{datePart}{nextSeq:D3}";

        // 3. Build entity with nested issue
        var stockIn = new StockIn
        {
            Code = stockInCode,
            PartId = request.PartId,
            SupplyQty = request.SupplyQty,
            SupplyDate = request.SupplyDate,
            ReceiptQty = request.ReceiptQty,
            ReceiptDate = request.ReceiptDate,
            CreatedAt = DateTime.UtcNow
        };

        stockIn.Issues.Add(new Issue
        {
            Number = issueNumber,
            CreatedAt = DateTime.UtcNow
        });

        await _stockInRepository.AddAsync(stockIn, cancellationToken);
        await _stockInRepository.SaveChangesAsync(cancellationToken);

        // 4. Reload with navigation (Part + Issues eager-loaded)
        var saved = await _stockInRepository.GetByIdAsync(stockIn.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(StockIn), stockIn.Id);

        var dto = saved.Adapt<StockInDto>();

        // 5. Trigger print label - Refactored to PrinterService with background execution
        await _printerService.PrintLabelStockIn(dto);

        return dto;
    }
}
