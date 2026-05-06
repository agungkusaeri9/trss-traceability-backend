using Microsoft.AspNetCore.Mvc;
using TraceabilitySystem.Application.DTOs.Part;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Shared.Helpers;
using TraceabilitySystem.Shared.Models;

namespace TraceabilitySystem.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PartController :  ControllerBase
{
    private readonly IPartService _partService;

    public PartController(IPartService partService)
    {
        _partService = partService;
    }
    
    [HttpGet]
    [ProducesResponseType(typeof(PagedApiResponse<PartDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetParts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _partService.GetPartsAsync(page, pageSize, search, cancellationToken);
        return ResponseFormatter.PagedSuccess(result);
    }
    
}