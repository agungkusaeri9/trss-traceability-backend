using Mapster;
using TraceabilitySystem.Application.DTOs.Part;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Domain.Interfaces;
using TraceabilitySystem.Shared.Exceptions;

namespace TraceabilitySystem.Application.Services;

public class ProcessCategoryService : IProcessCategoryService
{
    private readonly IProcessCategoryRepository _processCategoryRepository;

    public ProcessCategoryService(IProcessCategoryRepository processCategoryRepository)
    {
        _processCategoryRepository = processCategoryRepository;
    }

    /// <summary>
    /// Returns all Parts registered in the ProcessCategory named "clinching".
    /// </summary>
    public async Task<IEnumerable<PartDto>> GetPartsByClinchingAsync(CancellationToken cancellationToken = default)
    {
        var processCategory = await _processCategoryRepository.GetByClinchingWithPartsAsync(cancellationToken)
            ?? throw new NotFoundException("ProcessCategory", "clinching");

        var parts = processCategory.ProcessCategoryParts
            .Select(pcp => pcp.Part)
            .Where(p => p != null)
            .Select(p => p!.Adapt<PartDto>());

        return parts;
    }
}
