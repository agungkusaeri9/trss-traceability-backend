using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TraceabilitySystem.Domain.Entities;

namespace TraceabilitySystem.Domain.Interfaces;

public interface ISerialNumberRepository : IRepository<SerialNumber>
{
    Task<SerialNumber?> GetWithRelatedAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<SerialNumber>> GetAllWithChildRelationsAsync(CancellationToken cancellationToken = default);
    Task CreateWithIssuesAsync(
        IEnumerable<SerialNumber> serialNumbers,
        IEnumerable<string> issueNumbers,
        CancellationToken cancellationToken = default);
}
