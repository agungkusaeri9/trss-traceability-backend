using TraceabilitySystem.Domain.Entities;

namespace TraceabilitySystem.Domain.Interfaces;

public interface IIssueRepository : IRepository<Issue>
{
    Task<Dictionary<string, bool>> CheckIssueNumbersAsync(
        IEnumerable<string> issueNumbers,
        CancellationToken cancellationToken = default);
}
