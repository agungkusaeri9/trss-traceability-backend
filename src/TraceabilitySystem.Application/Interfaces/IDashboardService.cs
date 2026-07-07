using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TraceabilitySystem.Application.DTOs.Dashboard;
using TraceabilitySystem.Application.DTOs.ProcessLog;

namespace TraceabilitySystem.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
    Task<List<DashboardSummaryFieldDto>> GetTraceabilitySummaryAsync(CancellationToken cancellationToken = default);
    Task<DashboardStatsDto> GetStatsAsync(int topParts = 5,
    int trendDays = 7, CancellationToken cancellationToken = default);
    Task<List<ProcessLogDto>> GetRecentLogsAsync(int count = 5, CancellationToken cancellationToken = default);
    //Task<TraceabilityFlowDto> GetTraceabilityFlowAsync(CancellationToken cancellationToken = default);
}
