using Microsoft.EntityFrameworkCore;
using TraceabilitySystem.Domain.Entities;
using TraceabilitySystem.Domain.Interfaces;

namespace TraceabilitySystem.Infrastructure.Persistence.Repositories;

public class AppConfigRepository : BaseRepository<AppConfig>, IAppConfigRepository
{
    public AppConfigRepository(AppDbContext context) : base(context) { }

    public async Task<AppConfig?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(x => x.Key == key, cancellationToken);
    }
}
