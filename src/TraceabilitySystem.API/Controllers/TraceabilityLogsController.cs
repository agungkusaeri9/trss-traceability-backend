using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TraceabilitySystem.Application.DTOs.Pagination;
using TraceabilitySystem.Application.DTOs.ProcessLog;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Shared.Helpers;
using TraceabilitySystem.Shared.Models;

namespace TraceabilitySystem.API.Controllers;

[ApiController]
[Route("api/traceability-logs")]
public class TraceabilityLogsController : ControllerBase
{
    private readonly ITraceabilityLogService _traceabilityLogService;

    public TraceabilityLogsController(ITraceabilityLogService traceabilityLogService)
    {
        _traceabilityLogService = traceabilityLogService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedApiResponse<ProcessLogMockDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTraceabilityLogs(
        [FromQuery] PaginationDto pagination,
        [FromQuery] string? serialNumberCode = null,
        [FromQuery] bool? status = null,
        [FromQuery] bool? isFinished = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var page = pagination.Page < 1 ? 1 : pagination.Page;
        var limit = pagination.Limit < 1 ? 10 : pagination.Limit;

        var result = await _traceabilityLogService.GetTraceabilityLogsAsync(
            page, limit, serialNumberCode, status, isFinished, startDate, endDate, cancellationToken);

        return ResponseFormatter.PagedSuccess(result, "Traceability logs retrieved successfully.");
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<ProcessLogMockDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTraceabilityLog(long id, CancellationToken cancellationToken)
    {
        var result = await _traceabilityLogService.GetTraceabilityLogByIdAsync(id, cancellationToken);
        return ResponseFormatter.Success(result, "Traceability log retrieved successfully.");
    }

    [HttpGet("by-serial-number/{serialNumber}")]
    [ProducesResponseType(typeof(ApiResponse<ProcessLogMockDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTraceabilityLogBySerialNumber(string serialNumber, CancellationToken cancellationToken)
    {
        var result = await _traceabilityLogService.GetTraceabilityLogBySerialNumberAsync(serialNumber, cancellationToken);
        return ResponseFormatter.Success(result, "Traceability log retrieved successfully.");
    }

    [HttpGet("full-values/{serialNumberCode}")]
    [ProducesResponseType(typeof(ApiResponse<ProcessLogFullValueDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTraceabilityLogFullValuesAsync(string serialNumberCode, CancellationToken cancellationToken)
    {
        var result = await _traceabilityLogService.GetTraceabilityLogFullValuesAsync(serialNumberCode, cancellationToken);
        return ResponseFormatter.Success(result, "Traceability log full values retrieved successfully.");
    }
}
