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
        bool isFinished = true,
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

    Task<ProcessLog?> GetProcessLogFullValueAsync(
    string serialNumberCode,
    CancellationToken cancellationToken = default);

    Task<int> CountProductionAsync(DateTime startDate, DateTime endDate, bool? status, CancellationToken cancellationToken = default);
}
