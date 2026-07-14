using System.Text.Json.Serialization;
using FluentValidation;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Serilog;
using TraceabilitySystem.API.BackgroundServices;
using TraceabilitySystem.API.Extensions;
using TraceabilitySystem.API.Filters;
using TraceabilitySystem.API.Hubs;
using TraceabilitySystem.API.Middleware;
// using TraceabilitySystem.API.Services;
using TraceabilitySystem.Application;
using TraceabilitySystem.Application.Mappers;

// using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Infrastructure;
using TraceabilitySystem.Infrastructure.Persistence;
using TraceabilitySystem.Shared.Models;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddWindowsService();

    builder.WebHost.UseUrls(
    builder.Configuration["Server:Url"] ?? "http://0.0.0.0:5039");

    // ── Serilog ────────────────────────────────────────────────────────────
    builder.Host.UseSerilog((ctx, services, config) =>
    {
        config.ReadFrom.Configuration(ctx.Configuration)
              .ReadFrom.Services(services)
              .Enrich.FromLogContext();

        var customLogging = ctx.Configuration.GetSection("CustomLogging");
        bool debugIsTerminal = customLogging.GetValue<bool>("DebugIsTerminal", false);
        string logFolder = customLogging.GetValue<string>("LogFolder", "logging/logs");

        if (debugIsTerminal)
        {
            config.WriteTo.Async(a => a.Console());
        }
        else
        {
            // Menggunakan Map untuk memaksa format nama file yyyy-MM-dd
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
    });

    TypeAdapterConfig.GlobalSettings.Scan(typeof(StockInReworkMapping).Assembly);

    // ── Services ───────────────────────────────────────────────────────────
    builder.Services.AddControllers(opts =>
    {
        opts.Filters.Add<ValidationFilter>();
    })
     .AddJsonOptions(options =>
     {
         options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
         options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());

     });

    builder.Services.Configure<ApiBehaviorOptions>(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var message = string.Join("; ",
                context.ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));

            return new BadRequestObjectResult(
                ApiResponse.Fail(message));
        };
    });


    builder.Services.AddRouting(options => options.LowercaseUrls = true);

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.UseInlineDefinitionsForEnums();
    });
    builder.Services.AddJwtAuthentication(builder.Configuration);
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // ── CORS ───────────────────────────────────────────────────────────────
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.AllowAnyHeader()
                  .AllowAnyMethod()
                  .SetIsOriginAllowed(_ => true) // Mengizinkan semua origin
                  .AllowCredentials();
        });
    });

    // ── MQTT Settings ─────────────────────────────────────────────────────
    builder.Services.Configure<MqttSettings>(builder.Configuration.GetSection("MqttSettings"));

    // ── SignalR ────────────────────────────────────────────────────────────
    builder.Services.AddSignalR();
    // builder.Services.AddSingleton<ITraceabilitySummaryNotifier, TraceabilitySummaryNotifier>();
    builder.Services.AddHostedService<PrinterMonitorService>();
    // builder.Services.AddHostedService<TraceabilitySummaryBroadcastService>();

    // ── Build ──────────────────────────────────────────────────────────────
    var app = builder.Build();

    // ── Auto-migrate on startup ────────────────────────────────────────────
    // using (var scope = app.Services.CreateScope())
    // {
    //     var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    //     await db.Database.MigrateAsync();
    // }

    // ── Middleware pipeline ────────────────────────────────────────────────
    app.UseMiddleware<ExceptionMiddleware>();

    app.UseSwagger();
    app.UseSwaggerUI(c =>
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TraceabilitySystem API v1"));

    app.UseHttpsRedirection();

    var documentationPath = Path.GetFullPath(
        Path.Combine(app.Environment.ContentRootPath, "..", "..", "documentation"));
    if (Directory.Exists(documentationPath))
    {
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(documentationPath),
            RequestPath = "/documentation"
        });
    }

    app.UseStaticFiles();
    app.UseCors("AllowAll"); // Aktifkan CORS di sini
    app.UseSerilogRequestLogging();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    // ── SignalR Hubs ───────────────────────────────────────────────────────
    app.MapHub<PrinterHub>("/hubs/printer");
    app.MapHub<MqttStatusHub>("/hubs/mqtt-status");
    // app.MapHub<TraceabilitySummaryHub>("/hubs/traceability-summary");

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }

