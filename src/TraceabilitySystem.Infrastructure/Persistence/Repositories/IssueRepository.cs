using TraceabilitySystem.Domain.Entities;
using TraceabilitySystem.Domain.Interfaces;

namespace TraceabilitySystem.Infrastructure.Persistence.Repositories;

public class IssueRepository : BaseRepository<Issue>, IIssueRepository
{
    public IssueRepository(AppDbContext context) : base(context) { }
}
