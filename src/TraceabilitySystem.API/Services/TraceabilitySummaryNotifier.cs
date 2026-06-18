using Microsoft.AspNetCore.SignalR;
using TraceabilitySystem.API.Hubs;
using TraceabilitySystem.Application.Interfaces;

namespace TraceabilitySystem.API.Services;

public class TraceabilitySummaryNotifier : ITraceabilitySummaryNotifier
{
    private readonly IHubContext<TraceabilitySummaryHub> _hubContext;
    private readonly IServiceScopeFactory _scopeFactory;

    public TraceabilitySummaryNotifier(
        IHubContext<TraceabilitySummaryHub> hubContext,
        IServiceScopeFactory scopeFactory)
    {
        _hubContext = hubContext;
        _scopeFactory = scopeFactory;
    }

    public async Task BroadcastAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var dashboardService = scope.ServiceProvider.GetRequiredService<IDashboardService>();
        var summary = await dashboardService.GetTraceabilitySummaryAsync(cancellationToken);

        await _hubContext.Clients.All.SendAsync("TraceabilitySummaryUpdated", summary, cancellationToken);
    }
}
