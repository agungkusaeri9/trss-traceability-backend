using System.Threading;
using System.Threading.Tasks;
using TraceabilitySystem.Application.DTOs.Pagination;
using TraceabilitySystem.Application.DTOs.ProcessLog;
using TraceabilitySystem.Application.DTOs.TraceabilityLog;
using TraceabilitySystem.Domain.Entities;
using TraceabilitySystem.Shared.Models;

namespace TraceabilitySystem.Application.Interfaces;

public interface ITraceabilityLogService
{
    Task<PagedResult<ProcessLogMockDto>> GetTraceabilityLogsAsync(
        int page,
        int pageSize,
        string? serialNumberCode = null,
        bool? status = null,
        bool? isFinished = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);

    Task<ProcessLogMockDto> GetTraceabilityLogByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<ProcessLogMockDto> GetTraceabilityLogBySerialNumberAsync(string serialNumber, CancellationToken cancellationToken = default);

    Task<ProcessLogFullValueDto> GetTraceabilityLogFullValuesAsync(string serialNumberCode, CancellationToken cancellationToken = default);

    Task<TraceabilityLog> CreateNewAsync(CreateProcessLogRequestDto request, CancellationToken cancellationToken = default);

    Task<PagedResult<TraceabilityLogDto>> GetAllTraceabilityNewAsync(
        int page,
        int pageSize,
        string? search = null,
        bool? status = null,
        bool? isFinish = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);

    Task<TraceabilityLogDto> GetBySerialNumberClinchingNewAsync(
        string serialNumberClinching,
        CancellationToken cancellationToken = default);

    Task<List<TraceabilityLogDto>> GetRecentTraceabilityLogsAsync(
        int count = 10,
        CancellationToken cancellationToken = default);

    Task<TraceabilityLogIssueDto> GetIssuesBySerialNumberAsync(
        string serialNumber,
        bool status = false,
        CancellationToken cancellationToken = default);
}
