using Microsoft.AspNetCore.Mvc;
using TraceabilitySystem.Application.DTOs.AppConfig;
using TraceabilitySystem.Application.DTOs.Pagination;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Shared.Helpers;
using TraceabilitySystem.Shared.Models;

namespace TraceabilitySystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AppConfigsController : ControllerBase
{
    private readonly IAppConfigService _appConfigService;

    public AppConfigsController(IAppConfigService appConfigService)
    {
        _appConfigService = appConfigService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedApiResponse<AppConfigDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAppConfigs(
        [FromQuery] PaginationDto pagination,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _appConfigService.GetAppConfigsAsync(
            pagination.Page, pagination.Limit, search, cancellationToken);
        return ResponseFormatter.PagedSuccess(result);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<AppConfigDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAppConfig(int id, CancellationToken cancellationToken)
    {
        var result = await _appConfigService.GetAppConfigByIdAsync(id, cancellationToken);
        return ResponseFormatter.Success(result, "App config retrieved successfully.");
    }

    [HttpGet("key/{key}")]
    [ProducesResponseType(typeof(ApiResponse<AppConfigDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAppConfigByKey(string key, CancellationToken cancellationToken)
    {
        var result = await _appConfigService.GetAppConfigByKeyAsync(key, cancellationToken);
        return ResponseFormatter.Success(result, "App config retrieved successfully.");
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<AppConfigDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAppConfig(
        [FromBody] CreateAppConfigRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _appConfigService.CreateAppConfigAsync(request, cancellationToken);
        return ResponseFormatter.Success(result, "App config created successfully.", StatusCodes.Status201Created);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<AppConfigDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAppConfig(
        int id, [FromBody] UpdateAppConfigRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _appConfigService.UpdateAppConfigAsync(id, request, cancellationToken);
        return ResponseFormatter.Success(result, "App config updated successfully.");
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAppConfig(int id, CancellationToken cancellationToken)
    {
        await _appConfigService.DeleteAppConfigAsync(id, cancellationToken);
        return ResponseFormatter.Success(message: "App config deleted successfully.");
    }
}
