using TraceabilitySystem.Domain.Entities;
using TraceabilitySystem.Domain.Interfaces;
using TraceabilitySystem.Shared.Exceptions;
using Windows.Data.Xml.Dom;

namespace TraceabilitySystem.Infrastructure.Persistence.Repositories;

public class PartRepository : BaseRepository<Part>, IPartRepository
{
    public PartRepository(AppDbContext context) : base(context)
    {
    }

    public async Task RemoveAsync(Part entity, CancellationToken cancellationToken = default)
    {
        _context.Parts.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Part> GetDetailByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var part = await _context.Parts.FindAsync(new object[] { id }, cancellationToken);
        return part!;
    }
}