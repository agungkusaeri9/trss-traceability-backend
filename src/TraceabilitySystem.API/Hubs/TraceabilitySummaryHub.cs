using Microsoft.AspNetCore.SignalR;
using TraceabilitySystem.Application.Interfaces;

namespace TraceabilitySystem.API.Hubs;

/// <summary>
/// SignalR Hub for real-time dashboard traceability summary updates.
/// </summary>
public class TraceabilitySummaryHub : Hub
{
    private readonly IServiceScopeFactory _scopeFactory;

    public TraceabilitySummaryHub(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public override async Task OnConnectedAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var dashboardService = scope.ServiceProvider.GetRequiredService<IDashboardService>();
        var summary = await dashboardService.GetTraceabilitySummaryAsync();

        await Clients.Caller.SendAsync("TraceabilitySummaryUpdated", summary);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}
