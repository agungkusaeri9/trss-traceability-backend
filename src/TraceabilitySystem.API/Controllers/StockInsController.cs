using Microsoft.AspNetCore.Authorization;
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
[Route("api/stock-ins")]
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

    [Authorize(Roles = "admin,user")]
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<StockInDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStockIn(int id, [FromBody] UpdateStockInRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _stockInService.UpdateStockInAsync(id, request, cancellationToken);
        return ResponseFormatter.Success(result, "Stock-in updated successfully.");
    }

    [Authorize(Roles = "admin,user")]
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

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "admin,user")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteStockIn(int id, CancellationToken cancellationToken)
    {
        await _stockInService.DeleteStockInAsync(id, cancellationToken);
        return ResponseFormatter.Success(message: "Stock-in and associated issues deleted successfully.");
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
        var issueNumber = stockIn.Issues.Count > 0 ? stockIn.Issues[0].Number : "-";
        var issueNumbers = stockIn.Issues.Select(i => i.Number).ToList();

        await _printService.PrintClinchingShortSideAsync(issueNumber, issueNumbers);

        return ResponseFormatter.Success(message: "Stock-in label print triggered.");
    }

    /// <summary>
    /// Preview stock-in label as PDF (A5 landscape)
    /// </summary>
    //[HttpGet("{id:int}/preview")]
    //[ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    //[ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    //public async Task<IActionResult> PreviewStockInLabel(int id, CancellationToken cancellationToken)
    //{
    //    var stockIn = await _stockInService.GetStockInByIdAsync(id, cancellationToken);

    //    var pdfBytes = _printService.GeneratePdfForStockIn(stockIn);

    //    return File(pdfBytes, "application/pdf", $"IssueLabel_{stockIn.Code}.pdf");
    //}

    /// <summary>
    /// Preview stock-in label as PDF (by issue number)
    /// </summary>
    //[HttpGet("preview/{issueNumber}")]
    //[ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    //[ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    //public async Task<IActionResult> PreviewStockInLabelByIssueNumber(string issueNumber, CancellationToken cancellationToken)
    //{
    //    var stockIn = await _stockInService.GetStockInByIssueNumberAsync(issueNumber, cancellationToken);

    //    var pdfBytes = _printService.GeneratePdfForStockIn(stockIn);

    //    return File(pdfBytes, "application/pdf", $"IssueLabel_{stockIn.Code}.pdf");
    //}
}
