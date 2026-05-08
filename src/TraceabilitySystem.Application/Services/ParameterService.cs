using Mapster;
using TraceabilitySystem.Application.DTOs.Parameter;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Domain.Entities;
using TraceabilitySystem.Domain.Interfaces;
using TraceabilitySystem.Shared.Exceptions;
using TraceabilitySystem.Shared.Models;

namespace TraceabilitySystem.Application.Services;

public class ParameterService : BaseService<Parameter, ParameterDto>, IParameterService
{
    private readonly IParameterRepository _parameterRepository;

    public ParameterService(IParameterRepository repository) : base(repository)
    {
        _parameterRepository = repository;
    }

    public async Task<PagedResult<ParameterDto>> GetParametersAsync(
        int page, int pageSize, string? searchTerm = null, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        System.Linq.Expressions.Expression<Func<Parameter, bool>> predicate;

        if (!string.IsNullOrEmpty(searchTerm))
        {
            predicate = u => ((u.Name != null && u.Name.Contains(searchTerm)) || u.Code.Contains(searchTerm))
                            && (!isActive.HasValue || u.IsActive == isActive.Value);
        }
        else
        {
            predicate = u => !isActive.HasValue || u.IsActive == isActive.Value;
        }

        var (parameters, totalCount) = await _parameterRepository.GetPagedAsync(
            page,
            pageSize,
            predicate: predicate,
            orderBy: q => q.OrderByDescending(u => u.CreatedAt),
            cancellationToken: cancellationToken);

        return new PagedResult<ParameterDto>
        {
            Items = parameters.Adapt<IEnumerable<ParameterDto>>(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public Task<ParameterDto> GetParameterByIdAsync(int id, CancellationToken cancellationToken = default)
        => GetByIdAsync(id, cancellationToken);

    public async Task<ParameterDto> CreateParameterAsync(CreateParameterRequestDto request, CancellationToken cancellationToken = default)
    {
        bool exists = await _parameterRepository.ExistsAsync(p => p.Code == request.Code, cancellationToken);
        if (exists) throw new AppException("Code is already registered.", 409);

        var allowedTypes = new[] { "boolean", "text", "number" };
        if (!allowedTypes.Contains(request.DataType.ToLower()))
        {
            throw new AppException("Invalid DataType. Allowed values are: boolean, text, number.", 400);
        }

        var parameter = request.Adapt<Parameter>();
        parameter.DataType = request.DataType.ToLower();
        parameter.CreatedAt = DateTime.UtcNow;

        await _parameterRepository.AddAsync(parameter, cancellationToken);
        await _parameterRepository.SaveChangesAsync(cancellationToken);

        return parameter.Adapt<ParameterDto>();
    }

    public async Task<ParameterDto> UpdateParameterAsync(int id, UpdateParameterRequestDto request, CancellationToken cancellationToken = default)
    {
        var parameter = await _parameterRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Parameter), id);

        if (!string.IsNullOrWhiteSpace(request.Code) && request.Code != parameter.Code)
        {
            var exists = await _parameterRepository.ExistsAsync(
                p => p.Code == request.Code && p.Id != id, cancellationToken);
            if (exists) throw new AppException("Code is already in use.", 409);
            parameter.Code = request.Code;
        }

        if (request.Name is not null) parameter.Name = request.Name;
        if (request.Description is not null) parameter.Description = request.Description;
        
        if (request.DataType is not null)
        {
            var allowedTypes = new[] { "boolean", "text", "number" };
            if (!allowedTypes.Contains(request.DataType.ToLower()))
            {
                throw new AppException("Invalid DataType. Allowed values are: boolean, text, number.", 400);
            }
            parameter.DataType = request.DataType.ToLower();
        }

        if (request.IsActive.HasValue) parameter.IsActive = request.IsActive.Value;

        parameter.UpdatedAt = DateTime.UtcNow;

        _parameterRepository.Update(parameter);
        await _parameterRepository.SaveChangesAsync(cancellationToken);

        return parameter.Adapt<ParameterDto>();
    }

    public async Task ChangeStatusAsync(int id, bool isActive, CancellationToken cancellationToken = default)
    {
        var parameter = await _parameterRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Parameter), id);

        parameter.IsActive = isActive;
        parameter.UpdatedAt = DateTime.UtcNow;

        _parameterRepository.Update(parameter);
        await _parameterRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteParameterAsync(int id, CancellationToken cancellationToken = default)
    {
        await DeleteAsync(id, cancellationToken);
    }
}
