using TraceabilitySystem.Domain.Entities;
using TraceabilitySystem.Domain.Interfaces;

namespace TraceabilitySystem.Infrastructure.Persistence.Repositories;

public class ProcessRepository : BaseRepository<Process>, IProcessRepository
{
    public ProcessRepository(AppDbContext context) : base(context) { }
}
