using TraceabilitySystem.Application.DTOs.MqttPrintRequest;
using TraceabilitySystem.Shared.Models;

namespace TraceabilitySystem.Application.Interfaces;

public interface IMqttPrintRequestService
{
    Task<PagedResult<MqttPrintRequestDto>> GetAllPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<MqttPrintRequestDto> CreateAsync(CreateMqttPrintRequestDto request, CancellationToken cancellationToken = default);
    Task<MqttPrintRequestDto?> UpdateStatusAsync(int id, string status, string? errorMessage = null, CancellationToken cancellationToken = default);
    Task<MqttPrintRequestDto?> PrintAsync(int id, CancellationToken cancellationToken = default);
}
