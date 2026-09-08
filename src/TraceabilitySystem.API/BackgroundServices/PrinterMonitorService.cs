using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TraceabilitySystem.API.Helpers;
using TraceabilitySystem.API.Hubs;
using TraceabilitySystem.Domain.Interfaces;

namespace TraceabilitySystem.API.BackgroundServices;

/// <summary>
/// Background service that periodically checks the 3 AppConfig printers connectivity 
/// (Stock In, Clinching, M-Fan Assy) and broadcasts the status to all connected SignalR clients.
/// </summary>
public class PrinterMonitorService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<PrinterHub> _hubContext;
    private readonly ILogger<PrinterMonitorService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(15); // Ping interval

    public PrinterMonitorService(
        IServiceScopeFactory scopeFactory,
        IHubContext<PrinterHub> hubContext,
        ILogger<PrinterMonitorService> logger)
    {
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PrinterMonitorService started. Checking 3 AppConfig printers every {Interval}s.", _interval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndBroadcastAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking printer statuses.");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task CheckAndBroadcastAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var configRepo = scope.ServiceProvider.GetRequiredService<IAppConfigRepository>();

        var statuses = await PrinterChecker.CheckAppConfigPrintersAsync(configRepo, cancellationToken);

        await _hubContext.Clients.All.SendAsync("PrinterStatusUpdated", statuses, cancellationToken);
    }
}

