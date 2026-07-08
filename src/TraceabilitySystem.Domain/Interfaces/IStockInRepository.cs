using TraceabilitySystem.Domain.Entities;

namespace TraceabilitySystem.Domain.Interfaces;

public interface IStockInRepository : IRepository<StockIn>
{
    Task<StockIn?> GetByIssueNumberAsync(string issueNumber, CancellationToken cancellation = default);
}
