using Microsoft.AspNetCore.Mvc;
using TraceabilitySystem.Application.DTOs.MqttPrintRequest;
using TraceabilitySystem.Application.DTOs.Pagination;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Shared.Helpers;
using TraceabilitySystem.Shared.Models;

namespace TraceabilitySystem.API.Controllers;

[ApiController]
[Route("api/mqtt-print-requestes")]
public class MqttPrintRequestsController : ControllerBase
{
    private readonly IMqttPrintRequestService _mqttPrintRequestService;

    public MqttPrintRequestsController(IMqttPrintRequestService mqttPrintRequestService)
    {
        _mqttPrintRequestService = mqttPrintRequestService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedApiResponse<MqttPrintRequestDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] PaginationDto pagination,
        CancellationToken cancellationToken = default)
    {
        var result = await _mqttPrintRequestService.GetAllPagedAsync(
            pagination.Page,
            pagination.Limit,
            cancellationToken
        );

        return ResponseFormatter.PagedSuccess(result);
    }

    [HttpPost("{id:long}/print")]
    [ProducesResponseType(typeof(ApiResponse<MqttPrintRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Print(int id, CancellationToken cancellationToken = default)
    {
        var result = await _mqttPrintRequestService.PrintAsync(id, cancellationToken);
        return ResponseFormatter.Success(result, "Print request processed successfully.");
    }
}
