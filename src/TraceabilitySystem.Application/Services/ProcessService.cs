using Mapster;
using TraceabilitySystem.Application.DTOs.Process;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Domain.Entities;
using TraceabilitySystem.Domain.Interfaces;
using TraceabilitySystem.Shared.Exceptions;
using TraceabilitySystem.Shared.Models;

namespace TraceabilitySystem.Application.Services;

public class ProcessService : BaseService<Process, ProcessDto>, IProcessService
{
    private readonly IProcessRepository _processRepository;

    public ProcessService(IProcessRepository repository) : base(repository)
    {
        _processRepository = repository;
    }

    public async Task<PagedResult<ProcessDto>> GetProcessesAsync(
        int page, int pageSize, string? searchTerm = null, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        System.Linq.Expressions.Expression<Func<Process, bool>> predicate;

        if (!string.IsNullOrEmpty(searchTerm))
        {
            predicate = u => ((u.Name != null && u.Name.Contains(searchTerm)) || u.Code.Contains(searchTerm))
                            && (!isActive.HasValue || u.IsActive == isActive.Value);
        }
        else
        {
            predicate = u => !isActive.HasValue || u.IsActive == isActive.Value;
        }

        var (processes, totalCount) = await _processRepository.GetPagedAsync(
            page,
            pageSize,
            predicate: predicate,
            orderBy: q => q.OrderByDescending(u => u.CreatedAt),
            cancellationToken: cancellationToken);

        return new PagedResult<ProcessDto>
        {
            Items = processes.Adapt<IEnumerable<ProcessDto>>(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public Task<ProcessDto> GetProcessByIdAsync(int id, CancellationToken cancellationToken = default)
        => GetByIdAsync(id, cancellationToken);

    public async Task<ProcessDto> CreateProcessAsync(CreateProcessRequestDto request, CancellationToken cancellationToken = default)
    {
        bool exists = await _processRepository.ExistsAsync(p => p.Code == request.Code, cancellationToken);
        if (exists) throw new AppException("Code is already registered.", 409);

        var process = request.Adapt<Process>();
        process.CreatedAt = DateTime.UtcNow;

        await _processRepository.AddAsync(process, cancellationToken);
        await _processRepository.SaveChangesAsync(cancellationToken);

        return process.Adapt<ProcessDto>();
    }

    public async Task<ProcessDto> UpdateProcessAsync(int id, UpdateProcessRequestDto request, CancellationToken cancellationToken = default)
    {
        var process = await _processRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Process), id);

        if (!string.IsNullOrWhiteSpace(request.Code) && request.Code != process.Code)
        {
            var exists = await _processRepository.ExistsAsync(
                p => p.Code == request.Code && p.Id != id, cancellationToken);
            if (exists) throw new AppException("Code is already in use.", 409);
            process.Code = request.Code;
        }

        if (request.Name is not null) process.Name = request.Name;
        if (request.Description is not null) process.Description = request.Description;
        if (request.IsActive.HasValue) process.IsActive = request.IsActive.Value;

        process.UpdatedAt = DateTime.UtcNow;

        _processRepository.Update(process);
        await _processRepository.SaveChangesAsync(cancellationToken);

        return process.Adapt<ProcessDto>();
    }

    public async Task ChangeStatusAsync(int id, bool isActive, CancellationToken cancellationToken = default)
    {
        var process = await _processRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Process), id);

        process.IsActive = isActive;
        process.UpdatedAt = DateTime.UtcNow;

        _processRepository.Update(process);
        await _processRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteProcessAsync(int id, CancellationToken cancellationToken = default)
    {
        await DeleteAsync(id, cancellationToken);
    }
}
