using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Sockets;
using TraceabilitySystem.API.Hubs;
using TraceabilitySystem.Application.DTOs.Printer;
using TraceabilitySystem.Domain.Interfaces;

namespace TraceabilitySystem.API.BackgroundServices;

/// <summary>
/// Background service that periodically checks printer connectivity 
/// and broadcasts the status to all connected SignalR clients.
/// </summary>
public class PrinterMonitorService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<PrinterHub> _hubContext;
    private readonly ILogger<PrinterMonitorService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(10); // Ping interval

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
        _logger.LogInformation("PrinterMonitorService started. Checking every {Interval}s.", _interval.TotalSeconds);

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
        var printerRepository = scope.ServiceProvider.GetRequiredService<IPrinterRepository>();

        // Kita tarik SEMUA printer supaya yang sedang Offline tetap terpantau 
        // dan bisa balik jadi Online kalau sudah nyala.
        var printers = await printerRepository.GetAllAsync(cancellationToken);
        
        if (!printers.Any()) return;

        // Ping all printers concurrently
        var tasks = printers.Select(async printer =>
        {
            var isOnline = await PingPrinterAsync(printer.IpAddress, printer.Port);

            return new PrinterStatusDto
            {
                Id = printer.Id,
                Name = printer.Name,
                IpAddress = printer.IpAddress,
                Port = printer.Port,
                IsOnline = isOnline,
                Status = isOnline ? "Online" : "Offline",
                LastChecked = DateTime.Now
            };
        });

        var statuses = await Task.WhenAll(tasks);

        // ── UPDATE DATABASE ──────────────────────────────────────────────────
        // Kita update status IsActive di DB supaya sinkron dengan kondisi fisik
        foreach (var status in statuses)
        {
            var printer = printers.First(p => p.Id == status.Id);
            if (printer.IsActive != status.IsOnline)
            {
                printer.IsActive = status.IsOnline;
                printer.UpdatedAt = DateTime.UtcNow;
                printerRepository.Update(printer);
            }
        }
        await printerRepository.SaveChangesAsync(cancellationToken);
        // ─────────────────────────────────────────────────────────────────────

        // Hanya broadcast jika ada printer yang Offline
        var offlinePrinters = statuses.Where(s => !s.IsOnline).ToList();
        
        if (offlinePrinters.Any())
        {
            await _hubContext.Clients.All.SendAsync("PrinterStatusUpdated", offlinePrinters, cancellationToken);
            
            _logger.LogWarning("Broadcasted {Count} OFFLINE printers.", offlinePrinters.Count);
        }
        else
        {
            _logger.LogDebug("All printers are online. Skipping broadcast.");
        }
    }

    private static async Task<bool> PingPrinterAsync(string ipAddress, int port)
    {
        try
        {
            using var client = new TcpClient();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2)); // 2 sec timeout
            await client.ConnectAsync(ipAddress, port, cts.Token);
            return client.Connected;
        }
        catch
        {
            return false;
        }
    }
}
