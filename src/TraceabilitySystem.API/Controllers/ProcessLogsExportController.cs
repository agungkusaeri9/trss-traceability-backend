using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Shared.Helpers;
using TraceabilitySystem.Shared.Models;

namespace TraceabilitySystem.API.Controllers;

[ApiController]
[Route("api/process-logs")]
public class ProcessLogsExportController : ControllerBase
{
    private readonly IProcessLogExportService _processLogExportService;

    public ProcessLogsExportController(IProcessLogExportService processLogExportService)
    {
        _processLogExportService = processLogExportService;
    }

    [HttpGet("export")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportToExcel(
        [FromQuery] string? serialNumberCode = null,
        [FromQuery] bool? status = null,
        [FromQuery] bool isFinished = true,
        CancellationToken cancellationToken = default)
    {
        var result = await _processLogExportService.ExportToExcelAsync(serialNumberCode, status, isFinished, cancellationToken);
        return result;
    }
}
