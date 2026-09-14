using System.Text.Json.Serialization;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Serilog;
using TraceabilitySystem.API.Extensions;
using TraceabilitySystem.API.Filters;
using TraceabilitySystem.API.Hubs;
using TraceabilitySystem.API.Middleware;
using TraceabilitySystem.Application;
using TraceabilitySystem.Application.Mappers;
using TraceabilitySystem.Shared.Models;

// Trigger rebuild with latest DateTimeHelper

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(new WebApplicationOptions
    {
        Args = args,
        WebRootPath = null
    });

    // API Host Configuration
    builder.Services.AddWindowsService();

    builder.WebHost.UseUrls(
    builder.Configuration["Server:Url"] ?? "http://0.0.0.0:5039");

    // ── Serilog ────────────────────────────────────────────────────────────
    builder.Host.AddSerilogConfiguration(builder.Configuration);

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
    builder.Services.AddHostedService<TraceabilitySystem.API.BackgroundServices.PrinterMonitorService>();

    // ── Build ──────────────────────────────────────────────────────────────
    var app = builder.Build();

    // ── Middleware pipeline ────────────────────────────────────────────────
    app.UseMiddleware<CorrelationAndLogContextMiddleware>();
    app.UseSerilogRequestLogging(options =>
    {
        options.GetLevel = (httpContext, elapsed, ex) =>
        {
            if (ex is OperationCanceledException || httpContext.RequestAborted.IsCancellationRequested)
                return Serilog.Events.LogEventLevel.Debug;

            if (ex != null || httpContext.Response.StatusCode >= 500)
                return Serilog.Events.LogEventLevel.Error;

            if (httpContext.Response.StatusCode >= 400)
                return Serilog.Events.LogEventLevel.Warning;

            return Serilog.Events.LogEventLevel.Information;
        };
    });
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

    app.UseCors("AllowAll");
    app.UseAuthentication();

    // ── Enrich UserId after Authentication ──────────────────────────────────
    app.Use(async (context, next) =>
    {
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? context.User.FindFirst("sub")?.Value
                ?? context.User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
                ?? context.User.Identity?.Name
                ?? "Authenticated";

            using (Serilog.Context.LogContext.PushProperty("UserId", userId))
            {
                await next();
            }
        }
        else
        {
            await next();
        }
    });

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

