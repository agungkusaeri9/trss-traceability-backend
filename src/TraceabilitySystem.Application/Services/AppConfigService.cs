using Mapster;
using System.Linq.Expressions;
using TraceabilitySystem.Application.DTOs.AppConfig;
using TraceabilitySystem.Application.DTOs.Pagination;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Domain.Entities;
using TraceabilitySystem.Domain.Interfaces;
using TraceabilitySystem.Shared.Exceptions;
using TraceabilitySystem.Shared.Models;

namespace TraceabilitySystem.Application.Services;

public class AppConfigService : BaseService<AppConfig, AppConfigDto>, IAppConfigService
{
    private readonly IAppConfigRepository _appConfigRepository;

    public AppConfigService(IAppConfigRepository repository) : base(repository)
    {
        _appConfigRepository = repository;
    }

    public async Task<PagedResult<AppConfigDto>> GetAppConfigsAsync(
        int page,
        int pageSize,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        Expression<Func<AppConfig, bool>>? predicate = null;

        if (!string.IsNullOrEmpty(search))
        {
            predicate = x => x.Key.Contains(search) || (x.Description != null && x.Description.Contains(search));
        }

        var (items, totalCount) = await _appConfigRepository.GetPagedAsync(
            page,
            pageSize,
            predicate,
            q => q.OrderBy(x => x.Key),
            cancellationToken);

        return new PagedResult<AppConfigDto>
        {
            Items = items.Adapt<IEnumerable<AppConfigDto>>(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<AppConfigDto> GetAppConfigByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _appConfigRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(AppConfig), id);

        return entity.Adapt<AppConfigDto>();
    }

    public async Task<AppConfigDto> GetAppConfigByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        var entity = await _appConfigRepository.GetByKeyAsync(key, cancellationToken)
            ?? throw new NotFoundException(nameof(AppConfig), key);

        return entity.Adapt<AppConfigDto>();
    }

    public async Task<AppConfigDto> CreateAppConfigAsync(CreateAppConfigRequestDto request, CancellationToken cancellationToken = default)
    {
        var exists = await _appConfigRepository.ExistsAsync(x => x.Key == request.Key, cancellationToken);
        if (exists)
        {
            throw new AppException($"Config with key '{request.Key}' already exists.");
        }

        var config = new AppConfig
        {
            Key = request.Key,
            Value = request.Value,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow
        };

        await _appConfigRepository.AddAsync(config, cancellationToken);
        await _appConfigRepository.SaveChangesAsync(cancellationToken);

        return config.Adapt<AppConfigDto>();
    }

    public async Task<AppConfigDto> UpdateAppConfigAsync(int id, UpdateAppConfigRequestDto request, CancellationToken cancellationToken = default)
    {
        var config = await _appConfigRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(AppConfig), id);

        config.Value = request.Value;
        config.Description = request.Description;
        config.UpdatedAt = DateTime.UtcNow;

        _appConfigRepository.Update(config);
        await _appConfigRepository.SaveChangesAsync(cancellationToken);

        return config.Adapt<AppConfigDto>();
    }

    public async Task DeleteAppConfigAsync(int id, CancellationToken cancellationToken = default)
    {
        await DeleteAsync(id, cancellationToken);
    }
}
