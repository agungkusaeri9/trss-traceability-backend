using TraceabilitySystem.Application.DTOs.Part;
using TraceabilitySystem.Shared.Models;

namespace TraceabilitySystem.Application.Interfaces;

public interface IPartService
{
    Task<PagedResult<PartDto>> GetPartsAsync(int page, int pageSize, string? searchTerm = null, bool? isActive = null, CancellationToken cancellationToken = default);
    Task<PartDto> GetPartByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PartDto> CreatePartAsync(CreatePartRequestDto request, CancellationToken cancellationToken = default);
    Task<PartDto> UpdatePartAsync(int id, UpdatePartRequestDto request, CancellationToken cancellationToken = default);
    Task ChangeStatusAsync(int id, bool isActive, CancellationToken cancellationToken = default);
}