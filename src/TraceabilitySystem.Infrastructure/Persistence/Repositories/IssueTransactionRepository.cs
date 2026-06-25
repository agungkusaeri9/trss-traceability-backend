using TraceabilitySystem.Domain.Entities;
using TraceabilitySystem.Domain.Interfaces;

namespace TraceabilitySystem.Infrastructure.Persistence.Repositories;

public class IssueTransactionRepository : BaseRepository<IssueTransaction>, IIssueTransactionRepository
{
    public IssueTransactionRepository(AppDbContext context) : base(context) { }
}
