using Microsoft.EntityFrameworkCore;
using TraceabilitySystem.Domain.Entities;
using TraceabilitySystem.Domain.Interfaces;

namespace TraceabilitySystem.Infrastructure.Persistence.Repositories;

public class UserRepository : BaseRepository<User>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context) { }

    public async Task<User?> GetByEmailAsync(string Username, CancellationToken cancellationToken = default)
        => await _dbSet.FirstOrDefaultAsync(u => u.Username == Username, cancellationToken);

    public Task<(IEnumerable<User> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, string? searchTerm = null, CancellationToken cancellationToken = default)
    {
        return GetPagedAsync(
            page,
            pageSize,
            predicate: u => string.IsNullOrWhiteSpace(searchTerm)
                || u.Name.Contains(searchTerm)
                || u.Username.Contains(searchTerm),
            orderBy: q => q.OrderByDescending(u => u.CreatedAt),
            cancellationToken: cancellationToken);
    }
}
