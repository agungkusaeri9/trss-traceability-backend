using TraceabilitySystem.Application.DTOs.StockInRework;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Domain.Entities;
using TraceabilitySystem.Domain.Interfaces;
using TraceabilitySystem.Shared.Exceptions;
using TraceabilitySystem.Shared.Models;

namespace TraceabilitySystem.Application.Services;

public class StockInReworkService : IStockInReworkService
{
    private readonly IStockInReworkRepository _repository;

    public StockInReworkService(IStockInReworkRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedResult<StockInReworkDto>> GetPagedAsync(
        int page,
        int pageSize,
        long? serialNumberId = null,
        CancellationToken cancellationToken = default)
    {
        var predicate = serialNumberId.HasValue
            ? (System.Linq.Expressions.Expression<Func<StockInRework, bool>>)(x => x.SerialNumberId == serialNumberId.Value)
            : null;

        var (items, totalCount) = await _repository.GetPagedAsync(page, pageSize, predicate, cancellationToken: cancellationToken);

        var dtos = items.Select(MapToDto).ToList();
        return new PagedResult<StockInReworkDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<StockInReworkDto> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity == null) throw new NotFoundException(nameof(StockInRework), id);
        return MapToDto(entity);
    }

    public async Task<IEnumerable<StockInReworkDto>> CreateAsync(CreateStockInReworkDto dto, CancellationToken cancellationToken = default)
    {
        var entities = new List<StockInRework>();

        foreach (var issue in dto.IssueNumbers)
        {
            var entity = new StockInRework
            {
                SerialNumberId    = dto.SerialNumberId,
                IssueNumberBefore = issue.IssueNumber,
                IssueNumberAfter  = $"{issue.IssueNumber}-R",
                Qty               = issue.Qty,
                Note              = issue.Note,
                Status            = issue.Status,
                CreatedAt         = DateTime.UtcNow
            };
            entities.Add(entity);
            await _repository.AddAsync(entity, cancellationToken);
        }

        await _repository.SaveChangesAsync(cancellationToken);

        return entities.Select(MapToDto).ToList();
    }

    public async Task<StockInReworkDto> UpdateAsync(long id, UpdateStockInReworkDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity == null) throw new NotFoundException(nameof(StockInRework), id);

        if (dto.IssueNumberBefore is not null) entity.IssueNumberBefore = dto.IssueNumberBefore;
        if (dto.IssueNumberAfter  is not null) entity.IssueNumberAfter  = dto.IssueNumberAfter;
        if (dto.Qty.HasValue)                  entity.Qty               = dto.Qty.Value;
        if (dto.Note              is not null) entity.Note              = dto.Note;
        if (dto.Status.HasValue)               entity.Status            = dto.Status.Value;

        entity.UpdatedAt = DateTime.UtcNow;

        _repository.Update(entity);
        await _repository.SaveChangesAsync(cancellationToken);

        return MapToDto(entity);
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity == null) throw new NotFoundException(nameof(StockInRework), id);

        _repository.Remove(entity);
        await _repository.SaveChangesAsync(cancellationToken);
    }

    private static StockInReworkDto MapToDto(StockInRework x) => new()
    {
        Id                = x.Id,
        SerialNumberId    = x.SerialNumberId,
        SerialNumberCode  = x.SerialNumber?.SerialNumberCode,
        IssueNumberBefore = x.IssueNumberBefore,
        IssueNumberAfter  = x.IssueNumberAfter,
        Qty               = x.Qty,
        Note              = x.Note,
        Status            = x.Status,
        CreatedAt         = x.CreatedAt,
        UpdatedAt         = x.UpdatedAt
    };
}
