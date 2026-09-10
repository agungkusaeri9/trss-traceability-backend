using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TraceabilitySystem.Domain.Entities;

namespace TraceabilitySystem.Domain.Interfaces;

public interface ITraceabilityLogRepository : IRepository<ProcessLog>
{
    Task<(IEnumerable<ProcessLog> Items, int TotalCount)> GetPagedLogsAsync(
        int page,
        int pageSize,
        string? serialNumberCode = null,
        bool? status = null,
        bool? isFinished = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        bool clinchingOnly = false,
        CancellationToken cancellationToken = default);

    Task<ProcessLog?> GetLogWithDetailsAsync(long id, CancellationToken cancellationToken = default);

    Task<ProcessLog?> GetLogBySerialNumberAsync(string serialNumber, CancellationToken cancellationToken = default);

    Task<ProcessLog?> GetProcessLogFullValueAsync(
        string serialNumberCode,
        CancellationToken cancellationToken = default);

    Task<TraceabilityLog> CreateNewAsync(
        TraceabilityLog traceabilityLog,
        CancellationToken cancellationToken = default);

    Task<TraceabilityLog?> GetTraceabilityLogByCodeAsync(
        string code,
        CancellationToken cancellationToken = default);

    Task<(IEnumerable<TraceabilityLog> Items, int TotalCount)> GetAllTraceabilityNewAsync(
        int page,
        int pageSize,
        string? search = null,
        bool? status = null,
        bool? isFinish = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);

    Task<TraceabilityLog?> GetBySerialNumberClinchingAsync(
        string serialNumberClinching,
        CancellationToken cancellationToken = default);

    Task<Dictionary<string, List<string>>> GetIssueNumbersBySerialNumbersAsync(
        IEnumerable<string> serialNumberCodes,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<TraceabilityLog>> GetRecentTraceabilityLogsAsync(
        int count = 10,
        CancellationToken cancellationToken = default);
}
