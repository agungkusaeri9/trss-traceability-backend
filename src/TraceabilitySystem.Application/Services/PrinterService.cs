using Mapster;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using TraceabilitySystem.Application.DTOs.StockIn;
using TraceabilitySystem.Application.DTOs.Pagination;
using TraceabilitySystem.Application.DTOs.Printer;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Domain.Entities;
using TraceabilitySystem.Domain.Interfaces;
using TraceabilitySystem.Shared.Exceptions;
using TraceabilitySystem.Shared.Models;

namespace TraceabilitySystem.Application.Services;

public class PrinterService : BaseService<Printer, PrinterDto>, IPrinterService
{
    private readonly IPrinterRepository _printerRepository;
    private readonly IAppConfigRepository _appConfigRepository;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PrinterService> _logger;

    public PrinterService(
        IPrinterRepository repository,
        IAppConfigRepository appConfigRepository,
        IServiceScopeFactory scopeFactory,
        ILogger<PrinterService> logger) : base(repository)
    {
        _printerRepository = repository;
        _appConfigRepository = appConfigRepository;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<PagedResult<PrinterDto>> GetPrintersAsync(
        int page,
        int pageSize,
        string? search = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        Expression<Func<Printer, bool>>? predicate = null;

        if (!string.IsNullOrEmpty(search) || isActive.HasValue)
        {
            predicate = p =>
                (string.IsNullOrEmpty(search) ||
                    p.Name.Contains(search) ||
                    p.IpAddress.Contains(search)) &&
                (!isActive.HasValue || p.IsActive == isActive.Value);
        }

        var (items, totalCount) = await _printerRepository.GetPagedAsync(
            page,
            pageSize,
            predicate,
            q => q.OrderBy(p => p.Name),
            cancellationToken);

        return new PagedResult<PrinterDto>
        {
            Items = items.Adapt<IEnumerable<PrinterDto>>(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PrinterDto> GetPrinterByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _printerRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Printer), id);

        return entity.Adapt<PrinterDto>();
    }

    public async Task<PrinterDto> GetPrinterByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var entity = await _printerRepository.FirstOrDefaultAsync(x => x.Name == name, cancellationToken)
            ?? throw new NotFoundException(nameof(Printer), name);

        return entity.Adapt<PrinterDto>();
    }

    public async Task<PrinterDto> CreatePrinterAsync(CreatePrinterRequestDto request, CancellationToken cancellationToken = default)
    {
        var printer = new Printer
        {
            Name = request.Name,
            IpAddress = request.IpAddress,
            Port = request.Port,
            Description = request.Description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _printerRepository.AddAsync(printer, cancellationToken);
        await _printerRepository.SaveChangesAsync(cancellationToken);

        return printer.Adapt<PrinterDto>();
    }

    public async Task<PrinterDto> UpdatePrinterAsync(int id, UpdatePrinterRequestDto request, CancellationToken cancellationToken = default)
    {
        var printer = await _printerRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Printer), id);

        printer.Name = request.Name;
        printer.IpAddress = request.IpAddress;
        printer.Port = request.Port;
        printer.Description = request.Description;
        printer.IsActive = request.IsActive;
        printer.UpdatedAt = DateTime.UtcNow;

        _printerRepository.Update(printer);
        await _printerRepository.SaveChangesAsync(cancellationToken);

        return printer.Adapt<PrinterDto>();
    }

    public async Task DeletePrinterAsync(int id, CancellationToken cancellationToken = default)
    {
        await DeleteAsync(id, cancellationToken);
    }

    public async Task<PrinterDto> GetClinchingPrinterAsync(CancellationToken cancellationToken = default)
    {
        var clinchingPrinter = await _appConfigRepository.GetByKeyAsync("PRINTER_NAME_CLINCHING", cancellationToken);

        return await GetPrinterByNameAsync(clinchingPrinter!.Value, cancellationToken);
    }

    public async Task<PrinterDto> GetStockInPrinterAsync(CancellationToken cancellationToken = default)
    {
        var stockInPrinter = await _appConfigRepository.GetByKeyAsync("PRINTER_NAME_STOCK_IN", cancellationToken);

        return await GetPrinterByNameAsync(stockInPrinter!.Value, cancellationToken);
    }

    // public async Task PrintClinchingLabel(StockInDto stockIn)
    // {
    //     // Execute printing in background with proper logging
    //     _ = Task.Run(async () =>
    //     {
    //         using var scope = _scopeFactory.CreateScope();
    //         var printService = scope.ServiceProvider.GetRequiredService<IPrintService>();
    //         try
    //         {
    //             _logger.LogInformation("Starting print job for StockIn {Code}",
    //                 stockIn.Code);
    //             await printService.PrintStockInLabelWithSdkAsync(stockIn);
    //             _logger.LogInformation("Print job completed for StockIn {Code}", stockIn.Code);
    //         }
    //         catch (Exception ex)
    //         {
    //             _logger.LogError(ex, "Background printing failed for StockIn {Code}: {Error}",
    //                 stockIn.Code, ex.Message);
    //         }
    //     });
    // }
}
