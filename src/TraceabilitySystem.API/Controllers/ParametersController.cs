using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TraceabilitySystem.Application.DTOs.Parameter;
using TraceabilitySystem.Application.DTOs.Part;
using TraceabilitySystem.Application.DTOs.Pagination;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Shared.Helpers;
using TraceabilitySystem.Shared.Models;

namespace TraceabilitySystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ParametersController : ControllerBase
{
    private readonly IParameterService _parameterService;

    public ParametersController(IParameterService parameterService)
    {
        _parameterService = parameterService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedApiResponse<ParameterDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetParameters(
        [FromQuery] PaginationDto pagination,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _parameterService.GetParametersAsync(pagination.Page, pagination.Limit, search, isActive, cancellationToken);
        return ResponseFormatter.PagedSuccess(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ParameterDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateParameter(
        [FromBody] CreateParameterRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _parameterService.CreateParameterAsync(request, cancellationToken);
        return ResponseFormatter.Success(result, "Parameter created successfully.", StatusCodes.Status201Created);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<ParameterDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetParameter(int id, CancellationToken cancellationToken)
    {
        var result = await _parameterService.GetParameterByIdAsync(id, cancellationToken);
        return ResponseFormatter.Success(result, "Parameter retrieved successfully.");
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<ParameterDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateParameter(
        int id, [FromBody] UpdateParameterRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _parameterService.UpdateParameterAsync(id, request, cancellationToken);
        return ResponseFormatter.Success(result, "Parameter updated successfully.");
    }

    [HttpPatch("{id:int}/change-status")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeStatus(
        int id, [FromBody] ChangePartStatusRequestDto request, CancellationToken cancellationToken)
    {
        await _parameterService.ChangeStatusAsync(id, request.IsActive, cancellationToken);
        var statusMsg = request.IsActive ? "activated" : "deactivated";
        return ResponseFormatter.Success(message: $"Parameter {statusMsg} successfully.");
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteParameter(int id, CancellationToken cancellationToken)
    {
        await _parameterService.DeleteParameterAsync(id, cancellationToken);
        return ResponseFormatter.Success(message: "Parameter deleted successfully.");
    }
}
