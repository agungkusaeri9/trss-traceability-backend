using Serilog;
using System.Text.Json.Serialization;
using TraceabilitySystem.API.BackgroundServices;
using TraceabilitySystem.API.Extensions;
using TraceabilitySystem.API.Filters;
using TraceabilitySystem.API.Hubs;
using TraceabilitySystem.API.Middleware;
using TraceabilitySystem.Application;
using TraceabilitySystem.Infrastructure;
using TraceabilitySystem.Infrastructure.Persistence;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

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

    // ── Services ───────────────────────────────────────────────────────────
    builder.Services.AddControllers(opts =>
    {
        opts.Filters.Add<ValidationFilter>();
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

    builder.Services.AddRouting(options => options.LowercaseUrls = true);

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerConfiguration();
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

    // ── SignalR ────────────────────────────────────────────────────────────
    builder.Services.AddSignalR();
    builder.Services.AddHostedService<PrinterMonitorService>();

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

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "TraceabilitySystem API v1"));
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles(); // Tambahkan ini jika belum ada
    app.UseCors("AllowAll"); // Aktifkan CORS di sini
    app.UseSerilogRequestLogging();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    // ── SignalR Hubs ───────────────────────────────────────────────────────
    app.MapHub<PrinterHub>("/hubs/printer");

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

