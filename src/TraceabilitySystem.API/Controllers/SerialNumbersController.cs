using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TraceabilitySystem.Application.DTOs.Pagination;
using TraceabilitySystem.Application.DTOs.Parameter;
using TraceabilitySystem.Application.DTOs.SerialNumber;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Application.Services;
using TraceabilitySystem.Shared.Helpers;
using TraceabilitySystem.Shared.Models;

namespace TraceabilitySystem.API.Controllers
{
    [Route("api/serial-numbers")]
    [ApiController]
    public class SerialNumbersController : ControllerBase
    {

        private readonly ISerialNumberService _serialNumberSerivice;

        public SerialNumbersController(ISerialNumberService serialNumberService)
        {
            _serialNumberSerivice = serialNumberService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(PagedApiResponse<ParameterDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSerialNumbers(
            [FromQuery] PaginationDto pagination,
            [FromQuery] string? search = null,
            CancellationToken cancellationToken = default)
        {
            var result = await _serialNumberSerivice.GetSerialNumbersAsync(pagination.Page, pagination.Limit, search, cancellationToken);
            return ResponseFormatter.PagedSuccess(result);
        }

        [HttpGet("{serialNumber}")]
        [ProducesResponseType(typeof(ApiResponse<SerialNumberDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetSerialNumberByCodeAsync(string serialNumber, CancellationToken cancellationToken)
        {
            var result = await _serialNumberSerivice.GetBySerialNumberAsync(serialNumber, cancellationToken);
            return ResponseFormatter.Success(result, "Serial Number retrieved successfully.");
        }
    }
}
