using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TraceabilitySystem.Application.DTOs.Pagination;
using TraceabilitySystem.Application.DTOs.ProcessLog;
using TraceabilitySystem.Application.DTOs.TraceabilityLog;
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

    [HttpGet("/api/v2/traceability-logs")]
    [ProducesResponseType(typeof(PagedApiResponse<TraceabilityLogDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllTraceabilityV2(
        [FromQuery] PaginationDto pagination,
        [FromQuery] string? search = null,
        [FromQuery] bool? status = null,
        [FromQuery] bool? isFinish = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var page = pagination.Page < 1 ? 1 : pagination.Page;
        var limit = pagination.Limit < 1 ? 10 : pagination.Limit;

        var result = await _traceabilityLogService.GetAllTraceabilityNewAsync(
            page, limit, search, status, isFinish, startDate, endDate, cancellationToken);

        return ResponseFormatter.PagedSuccess(result, "Traceability logs retrieved successfully.");
    }

    [HttpGet("/api/v2/traceability-logs/by-serial-number/{serialNumber}")]
    [ProducesResponseType(typeof(ApiResponse<TraceabilityLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBySerialNumberClinchingV2(
        string serialNumber,
        CancellationToken cancellationToken = default)
    {
        var result = await _traceabilityLogService.GetBySerialNumberClinchingNewAsync(
            serialNumber, cancellationToken);

        return ResponseFormatter.Success(result, "Traceability log retrieved successfully.");
    }

    [HttpGet("/api/v2/traceability-logs/recents")]
    [ProducesResponseType(typeof(ApiResponse<List<TraceabilityLogDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecentTraceabilityLogs(
        [FromQuery] int count = 10,
        CancellationToken cancellationToken = default)
    {
        var limit = count <= 0 ? 10 : count;

        var result = await _traceabilityLogService.GetRecentTraceabilityLogsAsync(
            limit, cancellationToken);

        return ResponseFormatter.Success(result, "Recent traceability logs retrieved successfully.");
    }

    [HttpGet("/api/v2/traceability-logs/issues/by-serial-number/{serialNumber}")]
    [ProducesResponseType(typeof(ApiResponse<TraceabilityLogIssueDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetIssuesBySerialNumber(
        string serialNumber,
        [FromQuery] bool status = false,
        CancellationToken cancellationToken = default)
    {
        var result = await _traceabilityLogService.GetIssuesBySerialNumberAsync(serialNumber, status, cancellationToken);
        return ResponseFormatter.Success(result, "Traceability log issues retrieved successfully.");
    }
}

