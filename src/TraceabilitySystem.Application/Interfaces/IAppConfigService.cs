using TraceabilitySystem.Application.DTOs.AppConfig;
using TraceabilitySystem.Application.DTOs.Pagination;
using TraceabilitySystem.Domain.Entities;
using TraceabilitySystem.Shared.Models;

namespace TraceabilitySystem.Application.Interfaces;

public interface IAppConfigService : IBaseService<AppConfig, AppConfigDto>
{
    Task<PagedResult<AppConfigDto>> GetAppConfigsAsync(
        int page,
        int pageSize,
        string? search = null,
        CancellationToken cancellationToken = default);

    Task<AppConfigDto> GetAppConfigByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<AppConfigDto> GetAppConfigByKeyAsync(string key, CancellationToken cancellationToken = default);

    Task<AppConfigDto> CreateAppConfigAsync(CreateAppConfigRequestDto request, CancellationToken cancellationToken = default);

    Task<AppConfigDto> UpdateAppConfigAsync(int id, UpdateAppConfigRequestDto request, CancellationToken cancellationToken = default);

    Task DeleteAppConfigAsync(int id, CancellationToken cancellationToken = default);
}
