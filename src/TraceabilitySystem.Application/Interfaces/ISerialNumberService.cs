using System.Threading;
using System.Threading.Tasks;
using TraceabilitySystem.Application.DTOs.SerialNumber;

namespace TraceabilitySystem.Application.Interfaces;

public interface ISerialNumberService
{
    Task<SerialNumberDto> CreateAsync(CreateSerialNumberRequestDto request, CancellationToken cancellationToken = default);
    Task<IEnumerable<SerialNumberDto>> CreateBatchAsync(CreateBatchSerialNumberRequestDto request, CancellationToken cancellationToken = default);
    Task<IEnumerable<SerialNumberDto>> CreateFromIssuesAsync(CreateSerialNumbersFromIssuesRequestDto request, CancellationToken cancellationToken = default);
    Task<IEnumerable<SerialNumberDto>> CreateByClinchingAsync(GenerateSerialNumberRequestDto request, CancellationToken cancellationToken = default);
    Task<IEnumerable<SerialNumberDto>> CreateByMFanAsync(GenerateSerialNumberRequestDto request, CancellationToken cancellationToken = default);
    Task<SerialNumberDto?> GetBySerialNumberAsync(string serialNumber, CancellationToken cancellationToken = default);
}
