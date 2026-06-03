using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;
using TraceabilitySystem.Application.DTOs.Pagination;
using TraceabilitySystem.Application.DTOs.Printer;
using TraceabilitySystem.Application.DTOs.StockIn;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Shared.Helpers;
using TraceabilitySystem.Shared.Models;

namespace TraceabilitySystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PrintersController : ControllerBase
{
    private readonly IPrinterService _printerService;
    private readonly IPrintService _printService;

    public PrintersController(IPrinterService printerService, IPrintService printService)
    {
        _printerService = printerService;
        _printService = printService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedApiResponse<PrinterDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPrinters(
        [FromQuery] PaginationDto pagination,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _printerService.GetPrintersAsync(
            pagination.Page, pagination.Limit, search, isActive, cancellationToken);
        return ResponseFormatter.PagedSuccess(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<PrinterDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPrinter(int id, CancellationToken cancellationToken)
    {
        var result = await _printerService.GetPrinterByIdAsync(id, cancellationToken);
        return ResponseFormatter.Success(result, "Printer retrieved successfully.");
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PrinterDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreatePrinter(
        [FromBody] CreatePrinterRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _printerService.CreatePrinterAsync(request, cancellationToken);
        return ResponseFormatter.Success(result, "Printer created successfully.", StatusCodes.Status201Created);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<PrinterDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePrinter(
        int id, [FromBody] UpdatePrinterRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _printerService.UpdatePrinterAsync(id, request, cancellationToken);
        return ResponseFormatter.Success(result, "Printer updated successfully.");
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePrinter(int id, CancellationToken cancellationToken)
    {
        await _printerService.DeletePrinterAsync(id, cancellationToken);
        return ResponseFormatter.Success(message: "Printer deleted successfully.");
    }

    // [HttpPost("test-stockin")]
    // [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    // public async Task<IActionResult> TestStockInPrint([FromBody] StockInDto stockIn)
    // {
    //     await _printerService.PrintLabelStockIn(stockIn);
    //     return ResponseFormatter.Success(message: "Stock-In label print triggered. Check server logs for status.");
    // }

    /// <summary>
    /// Get raw ZPL string for a StockIn label
    /// Use http://labelary.com/viewer.html to preview the ZPL
    /// </summary>
    // [HttpPost("zpl-stockin")]
    // [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    // public IActionResult GetStockInZpl([FromBody] StockInDto stockIn)
    // {
    //     var zpl = _printService.GetZplForStockIn(stockIn);
    //     return ResponseFormatter.Success(zpl, "ZPL generated successfully.");
    // }
}
