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
    private readonly ISerialNumberRepository _serialNumberRepository;

    public StockInReworkService(
        IStockInReworkRepository repository,
        ISerialNumberRepository serialNumberRepository)
    {
        _repository = repository;
        _serialNumberRepository = serialNumberRepository;
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

    public async Task<StockInReworkDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity == null) throw new NotFoundException(nameof(StockInRework), id);
        return MapToDto(entity);
    }

    public async Task<IEnumerable<StockInReworkDto>> CreateAsync(CreateStockInReworkDto dto, CancellationToken cancellationToken = default)
    {
        var serialNumber = await _serialNumberRepository.FirstOrDefaultAsync(
            x => x.SerialNumberCode == dto.SerialNumberCode, cancellationToken);
            
        if (serialNumber == null)
            throw new AppException($"Serial number '{dto.SerialNumberCode}' not found.", 404);

        var resultEntities = new List<StockInRework>();

        foreach (var issue in dto.IssueNumbers)
        {
            // Cek apakah sudah ada record dengan serialNumberId + issueNumberBefore yang sama
            var existing = await _repository.FirstOrDefaultAsync(
                x => x.SerialNumberId == serialNumber.Id && x.IssueNumberBefore == issue.IssueNumber,
                cancellationToken);

            if (existing != null)
            {
                // Jika sudah ada, tambah qty
                existing.Qty += 1;
                existing.Note = issue.Note ?? existing.Note;
                existing.Status = issue.Status;
                existing.UpdatedAt = DateTime.UtcNow;
                _repository.Update(existing);
                resultEntities.Add(existing);
            }
            else
            {
                // Jika belum ada, insert baru dengan qty = 1
                var entity = new StockInRework
                {
                    SerialNumberId    = serialNumber.Id,
                    IssueNumberBefore = issue.IssueNumber,
                    IssueNumberAfter  = $"{issue.IssueNumber}-R",
                    Qty               = 1,
                    Note              = issue.Note,
                    Status            = issue.Status,
                    CreatedAt         = DateTime.UtcNow
                };
                await _repository.AddAsync(entity, cancellationToken);
                resultEntities.Add(entity);
            }
        }

        await _repository.SaveChangesAsync(cancellationToken);

        return resultEntities.Select(MapToDto).ToList();
    }

    public async Task<StockInReworkDto> UpdateAsync(int id, UpdateStockInReworkDto dto, CancellationToken cancellationToken = default)
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

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
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
