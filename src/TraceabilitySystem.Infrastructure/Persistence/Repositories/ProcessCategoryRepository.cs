using Microsoft.EntityFrameworkCore;
using TraceabilitySystem.Domain.Entities;
using TraceabilitySystem.Domain.Interfaces;

namespace TraceabilitySystem.Infrastructure.Persistence.Repositories;

public class ProcessCategoryRepository : BaseRepository<ProcessCategory>, IProcessCategoryRepository
{
    public ProcessCategoryRepository(AppDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Retrieves the ProcessCategory named "clinching" (case-insensitive)
    /// including all associated Parts.
    /// </summary>
    public async Task<ProcessCategory?> GetByClinchingWithPartsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ProcessCategories
            .Include(pc => pc.ProcessCategoryParts)
                .ThenInclude(pcp => pcp.Part)
            .Where(pc => pc.Name.ToLower() == "clinching")
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Retrieves the ProcessCategory named "mfan" (case-insensitive)
    /// including all associated Parts.
    /// </summary>
    public async Task<ProcessCategory?> GetByMfanWithPartsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ProcessCategories
            .Include(pc => pc.ProcessCategoryParts)
                .ThenInclude(pcp => pcp.Part)
            .Where(pc => pc.Name.ToLower() == "mfan")
            .FirstOrDefaultAsync(cancellationToken);
    }
}
