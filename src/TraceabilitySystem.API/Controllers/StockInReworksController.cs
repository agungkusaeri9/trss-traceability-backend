using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TraceabilitySystem.Application.DTOs.Pagination;
using TraceabilitySystem.Application.DTOs.StockInRework;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Shared.Helpers;
using TraceabilitySystem.Shared.Models;

namespace TraceabilitySystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StockInReworksController : ControllerBase
{
    private readonly IStockInReworkService _service;

    public StockInReworksController(IStockInReworkService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedApiResponse<StockInReworkDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList(
        [FromQuery] PaginationDto pagination,
        [FromQuery] long? serialNumberId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.GetPagedAsync(pagination.Page, pagination.Limit, serialNumberId, cancellationToken);
        return ResponseFormatter.PagedSuccess(result);
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<StockInReworkDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(id, cancellationToken);
        return ResponseFormatter.Success(result, "Stock in rework retrieved successfully.");
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<StockInReworkDto>>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateStockInReworkDto dto, CancellationToken cancellationToken)
    {
        var result = await _service.CreateAsync(dto, cancellationToken);
        return ResponseFormatter.Success(result, "Stock in rework created successfully.", StatusCodes.Status201Created);
    }

    [HttpPut("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<StockInReworkDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateStockInReworkDto dto, CancellationToken cancellationToken)
    {
        var result = await _service.UpdateAsync(id, dto, cancellationToken);
        return ResponseFormatter.Success(result, "Stock in rework updated successfully.");
    }

    [HttpDelete("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        await _service.DeleteAsync(id, cancellationToken);
        return ResponseFormatter.Success(message: "Stock in rework deleted successfully.");
    }
}
