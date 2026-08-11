using TraceabilitySystem.Domain.Entities;

namespace TraceabilitySystem.Domain.Interfaces;

public interface IProcessCategoryRepository : IRepository<ProcessCategory>
{
    Task<ProcessCategory?> GetByClinchingWithPartsAsync(CancellationToken cancellationToken = default);
    Task<ProcessCategory?> GetByMfanWithPartsAsync(CancellationToken cancellationToken = default);
}
