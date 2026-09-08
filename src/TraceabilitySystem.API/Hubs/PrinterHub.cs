using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using TraceabilitySystem.API.Helpers;
using TraceabilitySystem.Domain.Interfaces;

namespace TraceabilitySystem.API.Hubs;

/// <summary>
/// SignalR Hub for real-time printer connectivity monitoring (Stock In, Clinching, M-Fan Assy from AppConfig).
/// Clients connect here to receive live printer status updates.
/// </summary>
public class PrinterHub : Hub
{
    private readonly IServiceScopeFactory _scopeFactory;

    public PrinterHub(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public override async Task OnConnectedAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var configRepo = scope.ServiceProvider.GetRequiredService<IAppConfigRepository>();

        var statuses = await PrinterChecker.CheckAppConfigPrintersAsync(configRepo);

        await Clients.Caller.SendAsync("PrinterStatusUpdated", statuses);

        await base.OnConnectedAsync();
    }

    public async Task GetStatus()
    {
        using var scope = _scopeFactory.CreateScope();
        var configRepo = scope.ServiceProvider.GetRequiredService<IAppConfigRepository>();

        var statuses = await PrinterChecker.CheckAppConfigPrintersAsync(configRepo);

        await Clients.Caller.SendAsync("PrinterStatusUpdated", statuses);
    }
}
