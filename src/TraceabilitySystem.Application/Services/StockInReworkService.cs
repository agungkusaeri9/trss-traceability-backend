using System.Linq.Expressions;
using Mapster;
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
    FilterStockInReworkDto filter,
    CancellationToken cancellationToken = default)
    {
        //validation enum

        Expression<Func<StockInRework, bool>> predicate = x =>
    (!filter.SerialNumberId.HasValue || x.SerialNumberId == filter.SerialNumberId.Value) &&
    (!filter.Disposition.HasValue
        ? x.Disposition == DispositionType.PENDING.ToString()
        : x.Disposition == filter.Disposition.Value.ToString());

        var (items, totalCount) = await _repository.GetPagedAsync(
            page,
            pageSize,
            predicate,
            cancellationToken: cancellationToken);

        return new PagedResult<StockInReworkDto>
        {
            Items = items.Adapt<List<StockInReworkDto>>(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<StockInReworkDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity == null) throw new NotFoundException(nameof(StockInRework), id);
        return entity.Adapt<StockInReworkDto>();
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
                existing.Disposition = DispositionType.PENDING.ToString();
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
                    Disposition = DispositionType.PENDING.ToString(),
                    CreatedAt         = DateTime.UtcNow
                };
                await _repository.AddAsync(entity, cancellationToken);
                resultEntities.Add(entity);
            }
        }

        await _repository.SaveChangesAsync(cancellationToken);

        return resultEntities.Adapt<List<StockInReworkDto>>();
    }

    public async Task<StockInReworkDto> UpdateDispositionAsync(int id, UpdateStockInReworkDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);

        if (entity == null)
            throw new NotFoundException(nameof(StockInRework), id);


        if (entity.Disposition != DispositionType.PENDING.ToString())
        {
            throw new AppException(
                $"Only items with 'PENDING' disposition can be updated. Current disposition is '{entity.Disposition}'.",
                400);
        }



        entity.Disposition = dto.Disposition;
        entity.UpdatedAt = DateTime.UtcNow;

        _repository.Update(entity);
        await _repository.SaveChangesAsync(cancellationToken);

        return entity.Adapt<StockInReworkDto>();
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity == null) throw new NotFoundException(nameof(StockInRework), id);

        _repository.Remove(entity);
        await _repository.SaveChangesAsync(cancellationToken);
    }
}
