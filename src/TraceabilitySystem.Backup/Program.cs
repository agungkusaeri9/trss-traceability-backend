using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Serilog;
using TraceabilitySystem.Backup;
using TraceabilitySystem.Backup.BackgroundServices;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var host = Host.CreateDefaultBuilder(args)
        .UseWindowsService()
        .UseSerilog((ctx, services, config) =>
        {
            config.ReadFrom.Configuration(ctx.Configuration)
                  .ReadFrom.Services(services)
                  .Enrich.FromLogContext();

            var customLogging = ctx.Configuration.GetSection("CustomLogging");
            bool debugIsTerminal = customLogging.GetValue<bool>("DebugIsTerminal", false);
            string logFolder = customLogging.GetValue<string>("LogFolder", "logging/logs-backup")!;

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
                    .Filter.ByIncludingOnly(evt => evt.Level >= Serilog.Events.LogEventLevel.Error)
                    .WriteTo.Map(
                        le => le.Timestamp.ToString("yyyy-MM-dd"),
                        (date, wt) => wt.Async(a => a.File($"{logFolder}/errors/error-{date}.txt")),
                        sinkMapCountLimit: 2));
            }
        })
        .ConfigureServices((hostContext, services) =>
        {
            // Settings
            services.Configure<BackupSettings>(hostContext.Configuration.GetSection("BackupSettings"));

            // Background service
            services.AddHostedService<DatabaseBackupService>();
        })
        .Build();

    await host.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Backup host terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}
