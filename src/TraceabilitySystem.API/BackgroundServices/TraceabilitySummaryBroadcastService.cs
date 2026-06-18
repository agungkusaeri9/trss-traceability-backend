using TraceabilitySystem.Application.Interfaces;

namespace TraceabilitySystem.API.BackgroundServices;

/// <summary>
/// Reveals one traceability field every 3 seconds (value frozen on reveal).
/// After all fields including Final Inspection, waits 10 seconds then resets.
/// </summary>
public class TraceabilitySummaryBroadcastService : BackgroundService
{
    private readonly ITraceabilitySummarySimulator _simulator;
    private readonly ITraceabilitySummaryNotifier _notifier;
    private readonly ILogger<TraceabilitySummaryBroadcastService> _logger;

    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan RevealInterval = TimeSpan.FromSeconds(3);

    public TraceabilitySummaryBroadcastService(
        ITraceabilitySummarySimulator simulator,
        ITraceabilitySummaryNotifier notifier,
        ILogger<TraceabilitySummaryBroadcastService> logger)
    {
        _simulator = simulator;
        _notifier = notifier;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "TraceabilitySummaryBroadcastService started. Reveal every {Reveal}s, cooldown {Cooldown}s after complete.",
            RevealInterval.TotalSeconds,
            10);

        await _notifier.BroadcastAsync(stoppingToken);

        var revealElapsed = TimeSpan.Zero;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TickInterval, stoppingToken);

                if (_simulator.IsComplete)
                {
                    if (_simulator.TickCooldown())
                    {
                        _logger.LogInformation("Traceability summary cycle reset.");
                        await _notifier.BroadcastAsync(stoppingToken);
                    }

                    continue;
                }

                revealElapsed += TickInterval;

                if (revealElapsed < RevealInterval)
                {
                    continue;
                }

                revealElapsed = TimeSpan.Zero;

                if (_simulator.AdvanceReveal())
                {
                    _logger.LogDebug(
                        "Traceability field revealed: {Revealed}/{Total}",
                        _simulator.RevealedCount,
                        _simulator.TotalFields);

                    await _notifier.BroadcastAsync(stoppingToken);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error broadcasting traceability summary.");
            }
        }
    }
}
