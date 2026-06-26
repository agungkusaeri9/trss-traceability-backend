using Microsoft.EntityFrameworkCore;
using TraceabilitySystem.Domain.Entities;
using TraceabilitySystem.Domain.Interfaces;

namespace TraceabilitySystem.Infrastructure.Persistence.Repositories;

public class SerialNumberRepository : BaseRepository<SerialNumber>, ISerialNumberRepository
{
    public SerialNumberRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<SerialNumber?> GetWithRelatedAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(sn => sn.Issues!)
                .ThenInclude(sni => sni.Issue!)
                    .ThenInclude(i => i.StockIn!)
                        .ThenInclude(si => si.Part!)
            .FirstOrDefaultAsync(sn => sn.Id == id, cancellationToken);
    }
}
