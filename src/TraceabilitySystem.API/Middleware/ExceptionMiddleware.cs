using System.Net;
using System.Text.Json;
using TraceabilitySystem.Shared.Exceptions;
using TraceabilitySystem.Shared.Models;

namespace TraceabilitySystem.API.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(
        RequestDelegate next,
        ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Client menutup koneksi (refresh browser, navigasi, timeout client, dll).
            // Ini bukan error aplikasi sehingga tidak perlu di-log sebagai Error.
            _logger.LogDebug("Request was cancelled by the client.");

            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unhandled exception while processing {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            if (context.Response.HasStarted)
            {
                _logger.LogWarning(
                    "The response has already started, the exception middleware will not execute.");

                throw;
            }

            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(
        HttpContext context,
        Exception exception)
    {
        context.Response.Clear();
        context.Response.ContentType = "application/json";

        var (statusCode, response) = exception switch
        {
            NotFoundException ex =>
                (HttpStatusCode.NotFound,
                ApiResponse.Fail(ex.Message)),

            UnauthorizedException ex =>
                (HttpStatusCode.Unauthorized,
                ApiResponse.Fail(ex.Message)),

            ValidationException ex =>
                (HttpStatusCode.UnprocessableEntity,
                ApiResponse.Fail(ex.Message, ex.ValidationErrors)),

            AppException ex =>
                ((HttpStatusCode)ex.StatusCode,
                ApiResponse.Fail(ex.Message)),

            _ =>
                (HttpStatusCode.InternalServerError,
                ApiResponse.Fail(
                    "Terjadi kesalahan pada sistem. Silakan hubungi administrator apabila masalah masih berlanjut."))
        };

        context.Response.StatusCode = (int)statusCode;

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}