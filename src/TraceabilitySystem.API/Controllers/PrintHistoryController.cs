using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TraceabilitySystem.Application.DTOs.Pagination;
using TraceabilitySystem.Application.DTOs.Part;
using TraceabilitySystem.Application.DTOs.PrintHistory;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Shared.Helpers;
using TraceabilitySystem.Shared.Models;

namespace TraceabilitySystem.API.Controllers
{
    [Route("api/print-histories")]
    [ApiController]
    public class PrintHistoryController : ControllerBase
    {
        private readonly IPrintHistoryService _printHistoryService;
        private readonly IPrintService _printService;

        public PrintHistoryController(IPrintHistoryService printHistoryService, IPrintService printService)
        {
            _printHistoryService = printHistoryService;
            _printService = printService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(PagedApiResponse<PrintHistoryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllAsync(
            [FromQuery] PaginationDto pagination,
            [FromQuery] string? search = null,
            CancellationToken cancellationToken = default)
        {
            var result = await _printHistoryService.GetAllAsync(pagination.Page, pagination.Limit, search, cancellationToken);
            return ResponseFormatter.PagedSuccess(result);
        }

        [HttpPost("reprint/{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<PrintHistoryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> RePrint(int id, CancellationToken cancellationToken = default)
        {
            await _printService.RePrintAsync(id, cancellationToken);
            return ResponseFormatter.Success(message: "Reprint completed successfully.");
        }



    }
}
