using TraceabilitySystem.Domain.Entities;
using TraceabilitySystem.Domain.Interfaces;

namespace TraceabilitySystem.Infrastructure.Persistence.Repositories;

public class ParameterRepository : BaseRepository<Parameter>, IParameterRepository
{
    public ParameterRepository(AppDbContext context) : base(context)
    {
    }
}
