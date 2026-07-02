using TraceabilitySystem.Domain.Entities;

namespace TraceabilitySystem.Domain.Interfaces;

public interface IProcessRepository : IRepository<Process>
{
    Task<Dictionary<string, bool>> CheckParametersByProcessCodeAsync(
       string processCode,
       IEnumerable<string> parameterCodes,
       CancellationToken cancellationToken = default);
}
