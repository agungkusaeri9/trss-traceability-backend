using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;
using TraceabilitySystem.Application.DTOs.Pagination;
using TraceabilitySystem.Application.DTOs.StockIn;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Shared.Helpers;
using TraceabilitySystem.Shared.Models;

namespace TraceabilitySystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StockInsController : ControllerBase
{
    private readonly IStockInService _stockInService;
    private readonly IPrintService _printService;

    public StockInsController(IStockInService stockInService, IPrintService printService)
    {
        _stockInService = stockInService;
        _printService = printService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedApiResponse<StockInDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStockIns(
        [FromQuery] PaginationDto pagination,
        [FromQuery] DateTime? date = null,
        [FromQuery] string? issueNumber = null,
        [FromQuery] string? partNumber = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _stockInService.GetStockInsAsync(
            pagination.Page,
            pagination.Limit,
            date,
            issueNumber,
            partNumber,
            cancellationToken);

        return ResponseFormatter.PagedSuccess(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<StockInDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStockIn(int id, CancellationToken cancellationToken)
    {
        var result = await _stockInService.GetStockInByIdAsync(id, cancellationToken);
        return ResponseFormatter.Success(result, "Stock-in retrieved successfully.");
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<StockInDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateStockIn(
        [FromBody] CreateStockInRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _stockInService.CreateStockInAsync(request, cancellationToken);
        return ResponseFormatter.Success(result, "Stock-in created successfully.", StatusCodes.Status201Created);
    }

    /// <summary>
    /// Print stock-in label
    /// </summary>
    [HttpPost("{id:int}/print")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PrintStockIn(int id, CancellationToken cancellationToken)
    {
        var stockIn = await _stockInService.GetStockInByIdAsync(id, cancellationToken);

        await _printService.PrintStockInLabelWithSdkAsync(stockIn);

        return ResponseFormatter.Success(message: "Stock-in label print triggered.");
    }
}
