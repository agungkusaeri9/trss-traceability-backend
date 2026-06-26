using TraceabilitySystem.Domain.Entities;

namespace TraceabilitySystem.Domain.Interfaces;

public interface ISerialNumberRepository : IRepository<SerialNumber>
{
    Task<SerialNumber?> GetWithRelatedAsync(int id, CancellationToken cancellationToken = default);
}
