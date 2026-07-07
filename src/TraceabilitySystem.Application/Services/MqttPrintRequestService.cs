using Mapster;
using TraceabilitySystem.Application.DTOs.MqttPrintRequest;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Domain.Entities;
using TraceabilitySystem.Domain.Interfaces;
using TraceabilitySystem.Shared.Exceptions;
using TraceabilitySystem.Shared.Models;

namespace TraceabilitySystem.Application.Services;

public class MqttPrintRequestService : IMqttPrintRequestService
{
    private readonly IMqttPrintRequestRepository _repository;
    private readonly IPrintService _printService;

    public MqttPrintRequestService(IMqttPrintRequestRepository repository, IPrintService printService)
    {
        _repository = repository;
        _printService = printService;
    }

    public async Task<PagedResult<MqttPrintRequestDto>> GetAllPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(
            page,
            pageSize,
            orderBy: q => q.OrderByDescending(x => x.CreatedAt),
            cancellationToken: cancellationToken
        );

        return new PagedResult<MqttPrintRequestDto>
        {
            Items = items.Adapt<IEnumerable<MqttPrintRequestDto>>(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<MqttPrintRequestDto> CreateAsync(CreateMqttPrintRequestDto request, CancellationToken cancellationToken = default)
    {
        var entity = new MqttPrintRequest
        {
            ProcessCode = request.ProcessCode,
            IssueNumber = request.IssueNumber,
            RawPayload = request.RawPayload,
            Status = "Pending"
        };

        await _repository.AddAsync(entity, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return entity.Adapt<MqttPrintRequestDto>();
    }

    public async Task<MqttPrintRequestDto?> UpdateStatusAsync(int id, string status, string? errorMessage = null, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity == null)
            return null;

        entity.Status = status;
        entity.ErrorMessage = errorMessage;
        entity.UpdatedAt = DateTime.UtcNow;

        if (status == "Processed" || status == "Failed")
        {
            entity.ProcessedAt = DateTime.UtcNow;
        }

        _repository.Update(entity);
        await _repository.SaveChangesAsync(cancellationToken);

        return entity.Adapt<MqttPrintRequestDto>();
    }

    public async Task<MqttPrintRequestDto?> PrintAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        if (entity == null)
            throw new NotFoundException("MqttPrintRequest", id);

        try
        {
            if (entity.ProcessCode == "CLINCHING_SHORT_SIDE")
            {
                await _printService.PrintClinchingShortSideAsync(entity.IssueNumber, cancellationToken);
            }
            else if (entity.ProcessCode == "M_FAN_ASSY")
            {
                //await _printService.PrintMFanAssyAsync(entity.IssueNumber, cancellationToken);
            }

            entity.Status = "Processed";
            entity.ErrorMessage = null;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.ProcessedAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            entity.Status = "Failed";
            entity.ErrorMessage = ex.Message;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.ProcessedAt = DateTime.UtcNow;
        }

        _repository.Update(entity);
        await _repository.SaveChangesAsync(cancellationToken);

        return entity.Adapt<MqttPrintRequestDto>();
    }
}
