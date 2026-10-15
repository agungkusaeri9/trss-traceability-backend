using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TraceabilitySystem.Domain.Entities;

namespace TraceabilitySystem.Domain.Interfaces;

public interface IMqttPublisherStatusRepository : IRepository<MqttPublisherStatus>
{
    Task<List<MqttPublisherStatus>> GetByReferenceCodeAsync(
        string referenceCode,
        string referenceType = MqttPublisherReferenceTypes.ProcessLog,
        CancellationToken cancellationToken = default);

    Task<List<MqttPublisherStatus>> GetByReferenceCodesAsync(
        IEnumerable<string> referenceCodes,
        string referenceType = MqttPublisherReferenceTypes.ProcessLog,
        CancellationToken cancellationToken = default);

    Task SyncDisabledProcessesAsync(
        string referenceCode,
        string referenceType,
        IEnumerable<Process> relevantProcesses,
        CancellationToken cancellationToken = default);
}
