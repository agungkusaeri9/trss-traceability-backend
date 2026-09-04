using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using TraceabilitySystem.Backup;
using TraceabilitySystem.Backup.BackgroundServices;
using TraceabilitySystem.Shared.Constants;

const string outputTemplate =
    "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{Service}] [{Environment}] [{Category}] [Corr:{CorrelationId}] [Req:{RequestId}] [User:{UserId}] {Message:lj}{NewLine}{Exception}";

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: outputTemplate)
    .CreateBootstrapLogger();

try
{
    var host = Host.CreateDefaultBuilder(args)
        .UseWindowsService()
        .UseSerilog((ctx, services, config) =>
        {
            config.ReadFrom.Configuration(ctx.Configuration)
                  .ReadFrom.Services(services)
                  .Enrich.FromLogContext()
                  .Enrich.WithProperty("Service", "TraceabilitySystem.Backup")
                  .Enrich.WithProperty("Environment", ctx.HostingEnvironment.EnvironmentName)
                  .Enrich.With(new BackupStandardFieldsEnricher())
                  .Filter.ByExcluding(e => e.Exception is OperationCanceledException || e.Exception is TaskCanceledException);

            var customLogging = ctx.Configuration.GetSection("CustomLogging");
            bool debugIsTerminal = customLogging.GetValue<bool>("DebugIsTerminal", false);
            string logFolder = customLogging.GetValue<string>("LogFolder")
                ?? throw new InvalidOperationException("Configuration 'CustomLogging:LogFolder' is required in appsettings.json.");
            logFolder = Path.GetFullPath(logFolder);

            if (debugIsTerminal)
            {
                config.WriteTo.Async(a => a.Console(outputTemplate: outputTemplate));
            }
            else
            {
                config.WriteTo.Async(a => a.Console(outputTemplate: outputTemplate));

                config.WriteTo.Map(
                    le => le.Timestamp.ToString("yyyy-MM-dd"),
                    (date, wt) => wt.Async(a => a.File(
                        path: $"{logFolder}/{date}.txt",
                        outputTemplate: outputTemplate)),
                    sinkMapCountLimit: 2);
            }
        })
        .ConfigureServices((hostContext, services) =>
        {
            services.Configure<BackupSettings>(
                hostContext.Configuration.GetSection("BackupSettings"));

            services.AddHostedService<DatabaseBackupService>();
        })
        .Build();

    // ==========================
    // Startup Information
    // ==========================
    var configuration = host.Services.GetRequiredService<IConfiguration>();
    var environment = host.Services.GetRequiredService<IHostEnvironment>();

    var backupSettings = configuration
        .GetSection("BackupSettings")
        .Get<BackupSettings>();

    Log.Information("======================================================");
    Log.Information("Traceability Backup Service");
    Log.Information("======================================================");
    Log.Information("Environment     : {Environment}", environment.EnvironmentName);
    Log.Information("Machine         : {Machine}", Environment.MachineName);
    Log.Information(".NET Version    : {Version}", Environment.Version);
    Log.Information("Output Folder   : {Folder}", backupSettings?.OutputFolder);
    Log.Information("Interval Hours  : {Hours}", backupSettings?.IntervalHours);
    Log.Information("Retention Days  : {Days}", backupSettings?.RetentionDays);
    Log.Information("MySQL Host      : {Host}", backupSettings?.Host);
    Log.Information("MySQL Port      : {Port}", backupSettings?.Port);
    Log.Information("Database        : {Database}", backupSettings?.Database);
    Log.Information("MySQLDump Path  : {Path}", backupSettings?.MySqlDumpPath);
    Log.Information("Started At      : {Time}", DateTime.Now);
    Log.Information("======================================================");

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

/// <summary>
/// Fallback standard fields untuk process background backup
/// </summary>
sealed class BackupStandardFieldsEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (!logEvent.Properties.ContainsKey("Category"))
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("Category", LogCategory.BackgroundJob));

        if (!logEvent.Properties.ContainsKey("CorrelationId"))
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("CorrelationId", "-"));

        if (!logEvent.Properties.ContainsKey("RequestId"))
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("RequestId", "-"));

        if (!logEvent.Properties.ContainsKey("UserId"))
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("UserId", "System"));
    }
}