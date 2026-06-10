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
    [ProducesResponseType(typeof(PagedApiResponse<ProcessLogDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProcessLogs(
        [FromQuery] PaginationDto pagination,
        [FromQuery] string? issueNo = null,
        [FromQuery] string? partNumber = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _processLogService.GetProcessLogsAsync(
            pagination.Page, pagination.Limit, issueNo, partNumber, isActive, cancellationToken);
        return ResponseFormatter.PagedSuccess(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<ProcessLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProcessLog(int id, CancellationToken cancellationToken)
    {
        var result = await _processLogService.GetProcessLogByIdAsync(id, cancellationToken);
        return ResponseFormatter.Success(result, "Process log retrieved successfully.");
    }
}
