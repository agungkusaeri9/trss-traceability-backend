using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TraceabilitySystem.Domain.Entities;

namespace TraceabilitySystem.Domain.Interfaces;

public interface IProcessLogRepository : IRepository<ProcessLog>
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
    
    Task<IEnumerable<ProcessLog>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default);

    Task<ProcessLog> AddProcessLogPerProcessAsync(
        string serialNumberCode,
        string processCode,
        bool isOk,
        List<(string parameterCode, decimal? valueNumber, string? valueText, bool? valueBoolean, bool status)> parameters,
        CancellationToken cancellationToken = default);

    Task<int> CountProductionAsync(DateTime startDate, DateTime endDate, bool? status, CancellationToken cancellationToken = default);
    Task AddHistoriesAsync(IEnumerable<ProcessHistory> histories, CancellationToken cancellationToken = default);
    Task<List<ProcessHistory>> GetHistoriesBySerialNumberIdsAsync(IEnumerable<int> serialNumberIds, CancellationToken cancellationToken = default);
    Task<List<ProcessHistory>> GetHistoriesByReferenceCodesAsync(IEnumerable<string> referenceCodes, CancellationToken cancellationToken = default);
    Task<List<ProcessHistory>> GetHistoriesByTraceabilityLogIdAsync(long traceabilityLogId, CancellationToken cancellationToken = default);
    Task<List<ProcessHistory>> GetHistoriesAsync(
        IEnumerable<int>? serialNumberIds = null,
        IEnumerable<string>? referenceCodes = null,
        long? traceabilityLogId = null,
        long? processLogId = null,
        CancellationToken cancellationToken = default);
}
