using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using TraceabilitySystem.Shared.Constants;

namespace TraceabilitySystem.API.Extensions;

public static class HostExtensions
{
    private const string OutputTemplate =
        "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{Service}] [{Environment}] [{Category}] [Corr:{CorrelationId}] [Req:{RequestId}] [User:{UserId}] {Message:lj}{NewLine}{Exception}";

    public static ConfigureHostBuilder AddSerilogConfiguration(
        this ConfigureHostBuilder host,
        IConfiguration configuration)
    {
        host.UseSerilog((ctx, services, config) =>
        {
            config.ReadFrom.Configuration(ctx.Configuration)
                  .ReadFrom.Services(services)
                  .Enrich.FromLogContext()
                  .Enrich.WithProperty("Service", "TraceabilitySystem.API")
                  .Enrich.WithProperty("Environment", ctx.HostingEnvironment.EnvironmentName)
                  .Enrich.With(new StandardFieldsEnricher())
                  .Filter.ByExcluding(e => 
                      e.Exception is OperationCanceledException || 
                      e.Exception is TaskCanceledException ||
                      (e.MessageTemplate.Text.Contains("An error occurred using the connection to database") && 
                       (e.Exception == null || e.Exception is OperationCanceledException || e.Exception is TaskCanceledException)));

            var customLogging = configuration.GetSection("CustomLogging");
            bool debugIsTerminal = customLogging.GetValue<bool>("DebugIsTerminal", false);
            string logFolder = customLogging.GetValue<string>("LogFolder")
                ?? throw new InvalidOperationException("Configuration 'CustomLogging:LogFolder' is required in appsettings.json.");
            logFolder = Path.GetFullPath(logFolder);

            if (debugIsTerminal)
            {
                config.WriteTo.Async(a => a.Console(outputTemplate: OutputTemplate));
            }
            else
            {
                // Console juga tetap aktif untuk dev/monitoring
                config.WriteTo.Async(a => a.Console(outputTemplate: OutputTemplate));

                // File log harian (memuat seluruh level: Trace, Debug, Info, Warning, Error, Critical)
                config.WriteTo.Map(
                    le => le.Timestamp.ToString("yyyy-MM-dd"),
                    (date, wt) => wt.Async(a => a.File(
                        path: $"{logFolder}/{date}.txt",
                        outputTemplate: OutputTemplate)),
                    sinkMapCountLimit: 2);
            }
        });

        return host;
    }

    /// <summary>
    /// Memastikan field standar selalu memiliki nilai fallback jika tidak diinjeksi via LogContext
    /// </summary>
    private sealed class StandardFieldsEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            if (!logEvent.Properties.ContainsKey("Category"))
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("Category", LogCategory.Application));

            if (!logEvent.Properties.ContainsKey("CorrelationId"))
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("CorrelationId", "-"));

            if (!logEvent.Properties.ContainsKey("RequestId"))
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("RequestId", "-"));

            if (!logEvent.Properties.ContainsKey("UserId"))
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("UserId", "-"));
        }
    }
}