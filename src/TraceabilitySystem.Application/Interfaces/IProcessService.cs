using TraceabilitySystem.Application.DTOs.Process;
using TraceabilitySystem.Shared.Models;

namespace TraceabilitySystem.Application.Interfaces;

public interface IProcessService
{
    Task<PagedResult<ProcessDto>> GetProcessesAsync(int page, int pageSize, string? searchTerm = null, bool? isActive = null, CancellationToken cancellationToken = default);
    Task<ProcessDto> GetProcessByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ProcessDto> CreateProcessAsync(CreateProcessRequestDto request, CancellationToken cancellationToken = default);
    Task<ProcessDto> UpdateProcessAsync(int id, UpdateProcessRequestDto request, CancellationToken cancellationToken = default);
    Task ChangeStatusAsync(int id, bool isActive, CancellationToken cancellationToken = default);
    Task DeleteProcessAsync(int id, CancellationToken cancellationToken = default);
}
