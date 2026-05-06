using TraceabilitySystem.Domain.Entities;
using TraceabilitySystem.Domain.Interfaces;

namespace TraceabilitySystem.Infrastructure.Persistence.Repositories;

public class PartRepository : BaseRepository<Part>, IPartRepository
{
    public PartRepository(AppDbContext context) : base(context) { }
    
    
}