using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TraceabilitySystem.Application.DTOs.Dashboard;
using TraceabilitySystem.Application.DTOs.ProcessLog;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Shared.Helpers;
using TraceabilitySystem.Shared.Models;

namespace TraceabilitySystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("summary")]
    [ProducesResponseType(typeof(ApiResponse<DashboardSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var result = await _dashboardService.GetSummaryAsync(cancellationToken);
        return ResponseFormatter.Success(result, "Dashboard summary retrieved successfully.");
    }

    [HttpGet("traceability-summary")]
    [ProducesResponseType(typeof(ApiResponse<List<DashboardSummaryFieldDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTraceabilitySummary(CancellationToken cancellationToken)
    {
        var result = await _dashboardService.GetTraceabilitySummaryAsync(cancellationToken);
        return ResponseFormatter.Success(result, "Dashboard traceability summary retrieved successfully.");
    }

    [HttpGet("stats")]
    [ProducesResponseType(typeof(ApiResponse<DashboardStatsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStats(CancellationToken cancellationToken)
    {
        var result = await _dashboardService.GetStatsAsync(cancellationToken);
        return ResponseFormatter.Success(result, "Dashboard stats retrieved successfully.");
    }

    [HttpGet("recent-logs")]
    [ProducesResponseType(typeof(ApiResponse<List<ProcessLogDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRecentLogs(
        [FromQuery] int count = 5, 
        CancellationToken cancellationToken = default)
    {
        var result = await _dashboardService.GetRecentLogsAsync(count, cancellationToken);
        return ResponseFormatter.Success(result, "Recent logs retrieved successfully.");
    }
}
