using TraceabilitySystem.Application.DTOs.ProcessLog;
using TraceabilitySystem.Shared.Models;

namespace TraceabilitySystem.Application.Interfaces;

public interface IProcessLogService
{
    Task<PagedResult<ProcessLogDto>> GetProcessLogsAsync(
        int page, 
        int pageSize, 
        string? serialNumberCode = null, 
        string? partNumber = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default);

    Task<ProcessLogDto> GetProcessLogByIdAsync(long id, CancellationToken cancellationToken = default);
}
