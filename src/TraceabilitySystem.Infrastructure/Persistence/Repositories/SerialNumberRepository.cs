using TraceabilitySystem.Domain.Entities;
using TraceabilitySystem.Domain.Interfaces;

namespace TraceabilitySystem.Infrastructure.Persistence.Repositories;

public class SerialNumberRepository : BaseRepository<SerialNumber>, ISerialNumberRepository
{
    public SerialNumberRepository(AppDbContext context) : base(context)
    {
    }
}
