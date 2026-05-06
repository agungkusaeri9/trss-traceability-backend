using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;
using TraceabilitySystem.Application.DTOs.Pagination;
using TraceabilitySystem.Application.DTOs.Printer;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Shared.Helpers;
using TraceabilitySystem.Shared.Models;

namespace TraceabilitySystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PrintersController : ControllerBase
{
    private readonly IPrinterService _printerService;

    public PrintersController(IPrinterService printerService)
    {
        _printerService = printerService;
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
}
