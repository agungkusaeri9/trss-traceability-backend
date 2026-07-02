using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.IO;
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
        .UseWindowsService() // Allows running as a Windows Service
        .UseSerilog((ctx, services, config) =>
        {
            config.ReadFrom.Configuration(ctx.Configuration)
                  .ReadFrom.Services(services)
                  .Enrich.FromLogContext();

            var customLogging = ctx.Configuration.GetSection("CustomLogging");
            bool debugIsTerminal = customLogging.GetValue<bool>("DebugIsTerminal", false);
            string logFolder = customLogging.GetValue<string>("LogFolder", "logging/logs-worker");

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
            services.Configure<MqttSettings>(hostContext.Configuration.GetSection("MqttSettings"));
            services.Configure<WorkerSettings>(hostContext.Configuration.GetSection("WorkerSettings"));

            // Core layers
            services.AddApplication();
            services.AddInfrastructure(hostContext.Configuration);

            // HttpClient for SignalR Client
            services.AddHttpClient();

            // MQTT shared state (singleton so MqttPrintRequestService can set the client
            // and MqttPublisher can use it later from any scope)
            services.AddScoped<DatabaseService>();
            services.AddSingleton<MqttClientAccessor>();
            services.AddSingleton<IMqttPublisher, MqttPublisher>();
            services.AddScoped<MqttSubscriptionService>();
            services.AddScoped<IProcessValidator, ProcessValidator>();

            // Background worker
            services.AddHostedService<MqttWorkerService>();
        })
        .Build();

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
