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

    private static List<ProcessLogMockDto> GetMockData()
    {
        return ProcessLogMockData.GetMockRecords();
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedApiResponse<ProcessLogMockDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProcessLogs(
        [FromQuery] PaginationDto pagination,
        [FromQuery] string? serialNumberCode = null,
        [FromQuery] bool? status = null,
        [FromQuery] bool isFinished = true,
        CancellationToken cancellationToken = default)
    {
        var allMockData = GetMockData();

        var filtered = allMockData.Where(item =>
        {
            if (!string.IsNullOrWhiteSpace(serialNumberCode))
            {
                var matchesClinching = item.SerialNumberClinching.Contains(serialNumberCode, StringComparison.OrdinalIgnoreCase);
                var matchesMFan = !string.IsNullOrEmpty(item.SerialNumberMFan) && item.SerialNumberMFan.Contains(serialNumberCode, StringComparison.OrdinalIgnoreCase);
                var matchesGeneral = item.SerialNumber.Contains(serialNumberCode, StringComparison.OrdinalIgnoreCase);

                if (!matchesClinching && !matchesMFan && !matchesGeneral)
                {
                    return false;
                }
            }

            if (status.HasValue)
            {
                var isOk = string.Equals(item.OverallStatus, "PASSED", StringComparison.OrdinalIgnoreCase);
                if (isOk != status.Value)
                {
                    return false;
                }
            }

            return true;
        }).ToList();

        var page = pagination.Page < 1 ? 1 : pagination.Page;
        var limit = pagination.Limit < 1 ? 10 : pagination.Limit;
        var total = filtered.Count;
        var pagedItems = filtered.Skip((page - 1) * limit).Take(limit).ToList();

        var pagedResult = new PagedResult<ProcessLogMockDto>
        {
            Items = pagedItems,
            TotalCount = total,
            Page = page,
            PageSize = limit
        };

        return ResponseFormatter.PagedSuccess(pagedResult, "Process logs retrieved successfully.");
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<ProcessLogMockDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetProcessLog(long id, CancellationToken cancellationToken)
    {
        var allMockData = GetMockData();
        var found = allMockData.FirstOrDefault(item => item.Id == id);

        if (found == null)
        {
            return Task.FromResult<IActionResult>(ResponseFormatter.Error($"Process log with ID {id} was not found.", StatusCodes.Status404NotFound));
        }

        return Task.FromResult<IActionResult>(ResponseFormatter.Success(found, "Process log retrieved successfully."));
    }

    [HttpGet("by-serial-number/{serialNumber}")]
    [ProducesResponseType(typeof(ApiResponse<ProcessLogMockDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetProcessLogBySerialNumber(string serialNumber, CancellationToken cancellationToken)
    {
        var allMockData = GetMockData();
        var found = allMockData.FirstOrDefault(item => 
            string.Equals(item.SerialNumberClinching, serialNumber, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(item.SerialNumberMFan, serialNumber, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(item.SerialNumber, serialNumber, StringComparison.OrdinalIgnoreCase));

        if (found == null)
        {
            return Task.FromResult<IActionResult>(ResponseFormatter.Error($"Process log with serial number '{serialNumber}' was not found.", StatusCodes.Status404NotFound));
        }

        return Task.FromResult<IActionResult>(ResponseFormatter.Success(found, "Process log retrieved successfully."));
    }

    [HttpGet("full-values/{serialNumberCode}")]
    [ProducesResponseType(typeof(ApiResponse<ProcessLogMockDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetProcessLogFullValuesAsync(string serialNumberCode, CancellationToken cancellationToken)
    {
        var allMockData = GetMockData();
        var found = allMockData.FirstOrDefault(item => 
            string.Equals(item.SerialNumberClinching, serialNumberCode, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(item.SerialNumberMFan, serialNumberCode, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(item.SerialNumber, serialNumberCode, StringComparison.OrdinalIgnoreCase));

        if (found == null)
        {
            return Task.FromResult<IActionResult>(ResponseFormatter.Error($"Process log full values with serial number '{serialNumberCode}' was not found.", StatusCodes.Status404NotFound));
        }

        return Task.FromResult<IActionResult>(ResponseFormatter.Success(found, "Process log full values retrieved successfully."));
    }
}
