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
    private readonly IProcessLogService _processLogService;

    public StockInReworkService(
        IStockInReworkRepository repository,
        ISerialNumberRepository serialNumberRepository,
        IProcessLogService processLogService)
    {
        _repository = repository;
        _serialNumberRepository = serialNumberRepository;
        _processLogService = processLogService;
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
        var serialNumber = await GetAndValidateSerialNumberAsync(dto.SerialNumberCode, cancellationToken);

        await ValidateIssueNumbersExistAsync(dto.SerialNumberCode, dto.IssueNumbers, cancellationToken);
        await ValidateAllIssueNumbersIncludedAsync(dto.SerialNumberCode, dto.IssueNumbers, cancellationToken);
        await ValidateProcessLogStatusAsync(dto.SerialNumberCode, cancellationToken);

        var resultEntities = await UpsertStockInReworksAsync(serialNumber.Id, dto.IssueNumbers, cancellationToken);

        await _repository.SaveChangesAsync(cancellationToken);

        return resultEntities.Adapt<List<StockInReworkDto>>();
    }

    private async Task<SerialNumber> GetAndValidateSerialNumberAsync(
        string serialNumberCode,
        CancellationToken cancellationToken)
    {
        var serialNumber = await _serialNumberRepository.FirstOrDefaultAsync(
            x => x.SerialNumberCode == serialNumberCode, cancellationToken);

        if (serialNumber == null)
            throw new AppException($"Serial number '{serialNumberCode}' not found.", 404);

        return serialNumber;
    }

    private async Task<List<StockInRework>> UpsertStockInReworksAsync(
        int serialNumberId,
        List<IssueNumberRequestDto> issueNumbers,
        CancellationToken cancellationToken)
    {
        var resultEntities = new List<StockInRework>();

        foreach (var issue in issueNumbers)
        {
            var entity = await UpsertStockInReworkAsync(serialNumberId, issue, cancellationToken);
            resultEntities.Add(entity);
        }

        return resultEntities;
    }

    private async Task<StockInRework> UpsertStockInReworkAsync(
        int serialNumberId,
        IssueNumberRequestDto issueDto,
        CancellationToken cancellationToken)
    {
        var existing = await _repository.FirstOrDefaultAsync(
            x => x.SerialNumberId == serialNumberId && x.IssueNumberBefore == issueDto.IssueNumber,
            cancellationToken);

        if (existing != null)
            return UpdateExistingStockInRework(existing, issueDto);

        return CreateNewStockInRework(serialNumberId, issueDto);
    }

    private StockInRework UpdateExistingStockInRework(
        StockInRework entity,
        IssueNumberRequestDto issueDto)
    {
        entity.Qty += 1;
        entity.Note = issueDto.Note ?? entity.Note;
        entity.Status = issueDto.Status;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.Disposition = DispositionType.PENDING.ToString();
        _repository.Update(entity);
        return entity;
    }

    private StockInRework CreateNewStockInRework(
        int serialNumberId,
        IssueNumberRequestDto issueDto)
    {
        var entity = new StockInRework
        {
            SerialNumberId = serialNumberId,
            IssueNumberBefore = issueDto.IssueNumber,
            IssueNumberAfter = $"{issueDto.IssueNumber}-R",
            Qty = 1,
            Note = issueDto.Note,
            Status = issueDto.Status,
            Disposition = DispositionType.PENDING.ToString(),
            CreatedAt = DateTime.UtcNow
        };
        
        _repository.AddAsync(entity).GetAwaiter().GetResult();
        return entity;
    }

    private async Task ValidateIssueNumbersExistAsync(
        string serialNumberCode, 
        List<IssueNumberRequestDto> issueNumbers,
        CancellationToken cancellationToken)
    {
        var validIssueNumbers = await _serialNumberRepository.GetAllIssueNumbersByCodeAsync(serialNumberCode, cancellationToken);

        var invalidIssues = issueNumbers
            .Where(i => !validIssueNumbers.Contains(i.IssueNumber))
            .Select(i => i.IssueNumber)
            .ToList();

        if (invalidIssues.Count > 0)
            throw new AppException(
                $"Issue number(s) not found on serial number '{serialNumberCode}' or its related serial numbers: {string.Join(", ", invalidIssues)}",
                400);
    }

    private async Task ValidateAllIssueNumbersIncludedAsync(
        string serialNumberCode,
        List<IssueNumberRequestDto> issueNumbers,
        CancellationToken cancellationToken)
    {
        var validIssueNumbers = await _serialNumberRepository.GetAllIssueNumbersByCodeAsync(serialNumberCode, cancellationToken);
        var sentIssueNumbers = issueNumbers.Select(i => i.IssueNumber).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missingIssues = validIssueNumbers.Except(sentIssueNumbers, StringComparer.OrdinalIgnoreCase).ToList();

        if (missingIssues.Count > 0)
            throw new AppException(
                $"All issue numbers must be included. Missing: {string.Join(", ", missingIssues)}",
                400);
    }

    private async Task ValidateProcessLogStatusAsync(
        string serialNumberCode,
        CancellationToken cancellationToken)
    {
        var isValid = await _processLogService.ValidateSerialNumberProcessLogAsync(serialNumberCode, cancellationToken);
        if (!isValid)
            throw new AppException(
                $"Serial number '{serialNumberCode}' process log must have IsFinished = true and Status = false (NG).",
                400);
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
