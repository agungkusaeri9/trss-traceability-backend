using Microsoft.AspNetCore.Mvc;
using TraceabilitySystem.Application.DTOs.Part;
using TraceabilitySystem.Application.DTOs.Pagination;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Shared.Helpers;
using TraceabilitySystem.Shared.Models;

namespace TraceabilitySystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PartsController :  ControllerBase
{
    private readonly IPartService _partService;

    public PartsController(IPartService partService)
    {
        _partService = partService;
    }
    
    [HttpGet]
    [ProducesResponseType(typeof(PagedApiResponse<PartDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetParts(
        [FromQuery] PaginationDto pagination,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _partService.GetPartsAsync(pagination.Page, pagination.Limit, search, isActive, cancellationToken);
        return ResponseFormatter.PagedSuccess(result);
    }
    
    [HttpPost]
    // [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<PartDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreatePart(
        [FromBody] CreatePartRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _partService.CreatePartAsync(request, cancellationToken);
        return ResponseFormatter.Success(result, "Part created successfully.", StatusCodes.Status201Created);
    }
    
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<PartDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPart(int id, CancellationToken cancellationToken)
    {
        var result = await _partService.GetPartByIdAsync(id, cancellationToken);
        return ResponseFormatter.Success(result, "Part retrieved successfully.");
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<PartDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdatePart(
        int id, [FromBody] UpdatePartRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _partService.UpdatePartAsync(id, request, cancellationToken);
        return ResponseFormatter.Success(result, "Part updated successfully.");
    }

    [HttpPatch("{id:int}/change-status")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeStatus(
        int id, [FromBody] ChangePartStatusRequestDto request, CancellationToken cancellationToken)
    {
        await _partService.ChangeStatusAsync(id, request.IsActive, cancellationToken);
        var statusMsg = request.IsActive ? "activated" : "deactivated";
        return ResponseFormatter.Success(message: $"Part {statusMsg} successfully.");
    }
}