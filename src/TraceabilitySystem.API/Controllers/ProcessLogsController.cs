using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TraceabilitySystem.Application.DTOs.Pagination;
using TraceabilitySystem.Application.DTOs.ProcessLog;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Shared.Helpers;
using TraceabilitySystem.Shared.Models;

namespace TraceabilitySystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProcessLogsController : ControllerBase
{
    private readonly IProcessLogService _processLogService;

    public ProcessLogsController(IProcessLogService processLogService)
    {
        _processLogService = processLogService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedApiResponse<ProcessLogListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProcessLogs(
        [FromQuery] PaginationDto pagination,
        [FromQuery] string? serialNumberCode = null,

        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _processLogService.GetProcessLogsAsync(
            pagination.Page, pagination.Limit, serialNumberCode, isActive, cancellationToken);
        return ResponseFormatter.PagedSuccess(result);
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<ProcessLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProcessLog(long id, CancellationToken cancellationToken)
    {
        var result = await _processLogService.GetProcessLogByIdAsync(id, cancellationToken);
        return ResponseFormatter.Success(result, "Process log retrieved successfully.");
    }

    [HttpGet("by-serial-number/{serialNumber}")]
    [ProducesResponseType(typeof(ApiResponse<ProcessLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProcessLogBySerialNumber(string serialNumber, CancellationToken cancellationToken)
    {
        var result = await _processLogService.GetProcessLogBySerialNumberAsync(serialNumber, cancellationToken);
        return ResponseFormatter.Success(result, "Process log retrieved successfully.");
    }
}
