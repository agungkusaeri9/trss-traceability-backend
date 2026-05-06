using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TraceabilitySystem.Application.DTOs.Process;
using TraceabilitySystem.Application.DTOs.Part;
using TraceabilitySystem.Application.DTOs.Pagination;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Shared.Helpers;
using TraceabilitySystem.Shared.Models;

namespace TraceabilitySystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProcessesController : ControllerBase
{
    private readonly IProcessService _processService;

    public ProcessesController(IProcessService processService)
    {
        _processService = processService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedApiResponse<ProcessDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProcesses(
        [FromQuery] PaginationDto pagination,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _processService.GetProcessesAsync(pagination.Page, pagination.Limit, search, isActive, cancellationToken);
        return ResponseFormatter.PagedSuccess(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ProcessDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateProcess(
        [FromBody] CreateProcessRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _processService.CreateProcessAsync(request, cancellationToken);
        return ResponseFormatter.Success(result, "Process created successfully.", StatusCodes.Status201Created);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<ProcessDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProcess(int id, CancellationToken cancellationToken)
    {
        var result = await _processService.GetProcessByIdAsync(id, cancellationToken);
        return ResponseFormatter.Success(result, "Process retrieved successfully.");
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<ProcessDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateProcess(
        int id, [FromBody] UpdateProcessRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _processService.UpdateProcessAsync(id, request, cancellationToken);
        return ResponseFormatter.Success(result, "Process updated successfully.");
    }

    [HttpPatch("{id:int}/change-status")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeStatus(
        int id, [FromBody] ChangePartStatusRequestDto request, CancellationToken cancellationToken)
    {
        await _processService.ChangeStatusAsync(id, request.IsActive, cancellationToken);
        var statusMsg = request.IsActive ? "activated" : "deactivated";
        return ResponseFormatter.Success(message: $"Process {statusMsg} successfully.");
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProcess(int id, CancellationToken cancellationToken)
    {
        await _processService.DeleteProcessAsync(id, cancellationToken);
        return ResponseFormatter.Success(message: "Process deleted successfully.");
    }

    [HttpPost("{id:int}/parameters")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignParameters(
        int id, [FromBody] AdjustProcessParametersRequestDto request, CancellationToken cancellationToken)
    {
        await _processService.AssignParametersAsync(id, request, cancellationToken);
        return ResponseFormatter.Success(message: "Parameters successfully assigned to the process.");
    }

    [HttpDelete("{id:int}/parameters")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveParameters(
        int id, [FromBody] AdjustProcessParametersRequestDto request, CancellationToken cancellationToken)
    {
        await _processService.RemoveParametersAsync(id, request, cancellationToken);
        return ResponseFormatter.Success(message: "Parameters successfully removed from the process.");
    }
}
