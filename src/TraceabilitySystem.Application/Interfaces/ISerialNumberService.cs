using System.Threading;
using System.Threading.Tasks;
using TraceabilitySystem.Application.DTOs.Parameter;
using TraceabilitySystem.Application.DTOs.SerialNumber;
using TraceabilitySystem.Shared.Models;

namespace TraceabilitySystem.Application.Interfaces;

public interface ISerialNumberService
{
    Task<PagedResult<SerialNumberDto>> GetSerialNumbersAsync(int page, int pageSize, string? searchTerm = null, bool? status = null, bool isFinished = true, CancellationToken cancellationToken = default);
    Task<SerialNumberDto> CreateAsync(CreateSerialNumberRequestDto request, CancellationToken cancellationToken = default);
    Task<IEnumerable<SerialNumberDto>> CreateBatchAsync(CreateBatchSerialNumberRequestDto request, CancellationToken cancellationToken = default);
    Task<IEnumerable<SerialNumberDto>> CreateFromIssuesAsync(CreateSerialNumbersFromIssuesRequestDto request, CancellationToken cancellationToken = default);
    Task<IEnumerable<SerialNumberDto>> CreateByClinchingAsync(GenerateSerialNumberRequestDto request, CancellationToken cancellationToken = default);
    Task<IEnumerable<SerialNumberDto>> CreateByMFanAsync(GenerateSerialNumberRequestDto request, CancellationToken cancellationToken = default);
    Task<SerialNumberDto?> GetBySerialNumberAsync(string serialNumber, CancellationToken cancellationToken = default);
    Task<string> GenerateSerialNumberAsync(CancellationToken cancellationToken = default);
}
