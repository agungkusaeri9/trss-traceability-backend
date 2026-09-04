using System.Threading;
using System.Threading.Tasks;
using TraceabilitySystem.Application.DTOs.MqttLog;
using TraceabilitySystem.Application.DTOs.SystemLog;
using TraceabilitySystem.Shared.Models;

namespace TraceabilitySystem.Application.Interfaces;

public interface ILogService
{
    // MQTT Logs (Database)
    Task<PagedResult<MqttMessageLogDto>> GetMqttLogsAsync(
        MqttLogFilterDto filter,
        CancellationToken cancellationToken = default);

    Task<MqttMessageLogDto?> GetMqttLogByIdAsync(
        ulong id,
        CancellationToken cancellationToken = default);

    // System Logs (Files)
    Task<PagedResult<SystemLogDto>> GetSystemLogsAsync(
        SystemLogFilterDto filter,
        CancellationToken cancellationToken = default);

    Task<SystemLogDto?> GetSystemLogByIdAsync(
        string id,
        CancellationToken cancellationToken = default);
}
