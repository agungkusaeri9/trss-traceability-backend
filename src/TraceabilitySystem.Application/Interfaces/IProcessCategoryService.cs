using TraceabilitySystem.Application.DTOs.Part;

namespace TraceabilitySystem.Application.Interfaces;

public interface IProcessCategoryService
{
    Task<IEnumerable<PartDto>> GetPartsByClinchingAsync(CancellationToken cancellationToken = default);
}
