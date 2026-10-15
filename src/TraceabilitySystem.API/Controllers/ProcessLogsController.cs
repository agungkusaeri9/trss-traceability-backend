using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TraceabilitySystem.Application.DTOs.Pagination;
using TraceabilitySystem.Application.DTOs.ProcessLog;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Shared.Helpers;
using TraceabilitySystem.Shared.Models;

namespace TraceabilitySystem.API.Controllers;

[ApiController]
[Route("api/process-logs")]
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
        [FromQuery] bool? status = null,
        [FromQuery] bool? isFinished = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var page = pagination.Page < 1 ? 1 : pagination.Page;
        var limit = pagination.Limit < 1 ? 10 : pagination.Limit;

        var result = await _processLogService.GetProcessLogsAsync(
            page, limit, serialNumberCode, status, isFinished, startDate, endDate, cancellationToken);

        return ResponseFormatter.PagedSuccess(result, "Process logs retrieved successfully.");
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
