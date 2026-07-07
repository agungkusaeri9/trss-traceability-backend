using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TraceabilitySystem.Application.DTOs.Pagination;
using TraceabilitySystem.Application.DTOs.Parameter;
using TraceabilitySystem.Application.Interfaces;
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
        public async Task<IActionResult> GetParameters(
            [FromQuery] PaginationDto pagination,
            [FromQuery] string? search = null,
            CancellationToken cancellationToken = default)
        {
            var result = await _serialNumberSerivice.GetSerialNumbersAsync(pagination.Page, pagination.Limit, search, cancellationToken);
            return ResponseFormatter.PagedSuccess(result);
        }

        [HttpPost("generate")]
        [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GenerateTestData(CancellationToken cancellationToken = default)
        {
            var serialNumber = await _serialNumberSerivice.GenerateSerialNumberAsync(cancellationToken);
            return ResponseFormatter.Success(serialNumber, "Test serial number generated.");
        }
    }
}
