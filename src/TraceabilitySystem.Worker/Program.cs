using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Serilog;
using TraceabilitySystem.Application;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Infrastructure;
using TraceabilitySystem.Shared.Models;
using TraceabilitySystem.Worker.BackgroundServices;
using TraceabilitySystem.Worker.Services;
using TraceabilitySystem.Worker.Validator;

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
            string logFolder = customLogging.GetValue<string>("LogFolder", "logging/logs-worker")!;

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
            // Configurations
            services.Configure<MqttSettings>(
                hostContext.Configuration.GetSection("MqttSettings"));

            services.Configure<WorkerSettings>(
                hostContext.Configuration.GetSection("WorkerSettings"));

            // Core layers
            services.AddApplication();
            services.AddInfrastructure(hostContext.Configuration);

            // HttpClient
            services.AddHttpClient();

            // MQTT
            services.AddScoped<DatabaseService>();
            services.AddSingleton<MqttClientAccessor>();
            services.AddSingleton<IMqttPublisher, MqttPublisher>();
            services.AddScoped<MqttSubscriptionService>();
            services.AddScoped<IProcessValidator, ProcessValidator>();

            // Background Service
            services.AddHostedService<MqttWorkerService>();
        })
        .Build();

    // ==========================================================
    // Startup Information
    // ==========================================================
    var env = host.Services.GetRequiredService<IHostEnvironment>();
    var configuration = host.Services.GetRequiredService<IConfiguration>();

    var mqtt = host.Services
        .GetRequiredService<IOptions<MqttSettings>>()
        .Value;

    var worker = host.Services
        .GetRequiredService<IOptions<WorkerSettings>>()
        .Value;

    Log.Information("========================================================");
    Log.Information("         Traceability MQTT Worker Service");
    Log.Information("========================================================");
    Log.Information("Environment     : {Environment}", env.EnvironmentName);
    Log.Information("Machine Name    : {MachineName}", Environment.MachineName);
    Log.Information(".NET Version    : {DotNetVersion}", Environment.Version);
    Log.Information("OS              : {OS}", Environment.OSVersion);
    Log.Information("API URL         : {ApiUrl}", worker.ApiUrl);

    Log.Information("----------------------------------------");
    Log.Information("MQTT Configuration");
    Log.Information("----------------------------------------");
    Log.Information("Host            : {Host}", mqtt.Broker);
    Log.Information("Port            : {Port}", mqtt.Port);
    Log.Information("Client Id       : {ClientId}", mqtt.ClientId);
    //Log.Information("Username        : {Username}", mqtt.Username);

    Log.Information("----------------------------------------");
    Log.Information("Logging");
    Log.Information("----------------------------------------");
    Log.Information("Debug Terminal  : {Debug}",
        configuration.GetValue<bool>("CustomLogging:DebugIsTerminal"));
    Log.Information("Log Folder      : {Folder}",
        configuration["CustomLogging:LogFolder"]);

    Log.Information("----------------------------------------");
    Log.Information("Started At      : {StartedAt}",
        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
    Log.Information("========================================================");

    await host.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Worker host terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}

public class WorkerSettings
{
    public string ApiUrl { get; set; } = "http://localhost:5039";
}