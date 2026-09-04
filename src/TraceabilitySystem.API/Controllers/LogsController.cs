using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TraceabilitySystem.Application.DTOs.MqttLog;
using TraceabilitySystem.Application.DTOs.SystemLog;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Shared.Helpers;
using TraceabilitySystem.Shared.Models;

namespace TraceabilitySystem.API.Controllers;

[ApiController]
[Route("api/logs")]
public class LogsController : ControllerBase
{
    private readonly ILogService _logService;

    public LogsController(ILogService logService)
    {
        _logService = logService;
    }

    /// <summary>
    /// 1. List logs MQTT (Database)
    /// </summary>
    [HttpGet("mqtt")]
    [ProducesResponseType(typeof(PagedApiResponse<MqttMessageLogDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMqttLogs(
        [FromQuery] MqttLogFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var result = await _logService.GetMqttLogsAsync(filter, cancellationToken);
        return ResponseFormatter.PagedSuccess(result);
    }

    /// <summary>
    /// 2. Get by ID log MQTT (Database)
    /// </summary>
    [HttpGet("mqtt/{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<MqttMessageLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMqttLogById(
        ulong id,
        CancellationToken cancellationToken = default)
    {
        var result = await _logService.GetMqttLogByIdAsync(id, cancellationToken);
        if (result == null)
        {
            return ResponseFormatter.Error($"MQTT log with ID {id} was not found.");
        }

        return ResponseFormatter.Success(result, "MQTT log retrieved successfully.");
    }

    /// <summary>
    /// 3. List logs system (File: logs-api, logs-worker, logs-backup)
    /// </summary>
    [HttpGet("system")]
    [ProducesResponseType(typeof(PagedApiResponse<SystemLogDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSystemLogs(
        [FromQuery] SystemLogFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var result = await _logService.GetSystemLogsAsync(filter, cancellationToken);
        return ResponseFormatter.PagedSuccess(result);
    }

    /// <summary>
    /// 4. Get by ID log system (File)
    /// </summary>
    [HttpGet("system/{id}")]
    [ProducesResponseType(typeof(ApiResponse<SystemLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSystemLogById(
        string id,
        CancellationToken cancellationToken = default)
    {
        var result = await _logService.GetSystemLogByIdAsync(id, cancellationToken);
        if (result == null)
        {
            return ResponseFormatter.Error($"System log with ID '{id}' was not found.");
        }

        return ResponseFormatter.Success(result, "System log retrieved successfully.");
    }
}
