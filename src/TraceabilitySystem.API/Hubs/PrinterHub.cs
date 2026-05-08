using Microsoft.AspNetCore.SignalR;

namespace TraceabilitySystem.API.Hubs;

/// <summary>
/// SignalR Hub for real-time printer connectivity monitoring.
/// Clients connect here to receive live printer status updates.
/// </summary>
public class PrinterHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}
