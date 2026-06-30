using TraceabilitySystem.Application.DTOs.ProcessLog;
using TraceabilitySystem.Shared.Models;

namespace TraceabilitySystem.Application.Interfaces;

public interface IProcessLogService
{
    Task<PagedResult<ProcessLogListDto>> GetProcessLogsAsync(
        int page, 
        int pageSize, 
        string? serialNumberCode = null, 
        bool? isActive = null,
        CancellationToken cancellationToken = default);

    Task<ProcessLogDto> GetProcessLogByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<ProcessLogDto> GetProcessLogBySerialNumberAsync(string serialNumber, CancellationToken cancellationToken = default);

    Task<ProcessLogDto> CreateProcessLogWithDetailsAsync(
        CreateProcessLogRequestDto request,
        CancellationToken cancellationToken = default);

    Task<ProcessLogDto> CreateProcessLogDetailOnlyAsync(
        CreateProcessLogRequestDto request,
        CancellationToken cancellationToken = default);

    Task<ProcessLogDto> CreateProcessLogMFanAssyAsync(
        CreateProcessLogRequestDto request,
        string type,        
        CancellationToken cancellationToken = default);

    Task<ProcessLogDto> CreateProcessLogByClinchingAsync(
        CreateProcessLogRequestDto request,
        CancellationToken cancellationToken = default);
}
