using Serilog;
using Serilog.Events;

namespace TraceabilitySystem.API.Extensions;

public static class HostExtensions
{
    public static ConfigureHostBuilder AddSerilogConfiguration(
        this ConfigureHostBuilder host,
        IConfiguration configuration)
    {
        host.UseSerilog((ctx, services, config) =>
        {
            config.ReadFrom.Configuration(ctx.Configuration)
                  .ReadFrom.Services(services)
                  .Enrich.FromLogContext();

            var customLogging = configuration.GetSection("CustomLogging");
            bool debugIsTerminal = customLogging.GetValue<bool>("DebugIsTerminal", false);
            string logFolder = customLogging.GetValue<string>("LogFolder", "logging/logs");

            if (debugIsTerminal)
            {
                config.WriteTo.Async(a => a.Console());
            }
            else
            {
                config.WriteTo.Map(
                    le => le.Timestamp.ToString("yyyy-MM-dd"),
                    (date, wt) => wt.Async(a => a.File($"{logFolder}/{date}.txt")),
                    sinkMapCountLimit: 2);

                config.WriteTo.Logger(lc => lc
                    .Filter.ByIncludingOnly(e => e.Level >= LogEventLevel.Error)
                    .WriteTo.Map(
                        le => le.Timestamp.ToString("yyyy-MM-dd"),
                        (date, wt) => wt.Async(a => a.File($"{logFolder}/errors/error-{date}.txt")),
                        sinkMapCountLimit: 2));
            }
        });

        return host;
    }
}