using TraceabilitySystem.Application.DTOs.Parameter;
using TraceabilitySystem.Shared.Models;

namespace TraceabilitySystem.Application.Interfaces;

public interface IParameterService
{
    Task<PagedResult<ParameterDto>> GetParametersAsync(int page, int pageSize, string? searchTerm = null, bool? isActive = null, CancellationToken cancellationToken = default);
    Task<ParameterDto> GetParameterByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ParameterDto> CreateParameterAsync(CreateParameterRequestDto request, CancellationToken cancellationToken = default);
    Task<ParameterDto> UpdateParameterAsync(int id, UpdateParameterRequestDto request, CancellationToken cancellationToken = default);
    Task ChangeStatusAsync(int id, bool isActive, CancellationToken cancellationToken = default);
    Task DeleteParameterAsync(int id, CancellationToken cancellationToken = default);
}
