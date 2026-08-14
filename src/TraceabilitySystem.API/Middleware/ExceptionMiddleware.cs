using System.Net;
using System.Text.Json;
using TraceabilitySystem.Shared.Constants;
using TraceabilitySystem.Shared.Exceptions;
using TraceabilitySystem.Shared.Helpers;
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
        catch (OperationCanceledException)
        {
            // Client menutup koneksi (refresh browser, navigasi halaman di React, unmount component, dll).
            // Ini bukan error aplikasi sehingga tidak perlu di-log sebagai Error.
            _logger.LogWithCategory(LogLevel.Debug, LogCategory.API,
                "Request was cancelled by client for {Method} {Path}",
                context.Request.Method,
                context.Request.Path);

            return;
        }
        catch (NotFoundException ex)
        {
            _logger.LogWithCategory(LogLevel.Warning, LogCategory.API,
                "Resource not found for {Method} {Path}: {Message}",
                context.Request.Method,
                context.Request.Path,
                ex.Message);

            await HandleExceptionAsync(context, ex);
        }
        catch (ValidationException ex)
        {
            _logger.LogWithCategory(LogLevel.Warning, LogCategory.API,
                "Validation error for {Method} {Path}: {Message}",
                context.Request.Method,
                context.Request.Path,
                ex.Message);

            await HandleExceptionAsync(context, ex);
        }
        catch (UnauthorizedException ex)
        {
            _logger.LogWithCategory(LogLevel.Warning, LogCategory.Security,
                "Unauthorized access for {Method} {Path}: {Message}",
                context.Request.Method,
                context.Request.Path,
                ex.Message);

            await HandleExceptionAsync(context, ex);
        }
        catch (AppException ex)
        {
            _logger.LogWithCategory(LogLevel.Warning, LogCategory.API,
                "Application exception for {Method} {Path}: {Message}",
                context.Request.Method,
                context.Request.Path,
                ex.Message);

            await HandleExceptionAsync(context, ex);
        }
        catch (Exception ex)
        {
            _logger.LogWithCategory(LogLevel.Error, LogCategory.API, ex,
                "Unhandled server error while processing {Method} {Path}: {Message}",
                context.Request.Method,
                context.Request.Path,
                ex.Message);

            if (context.Response.HasStarted)
            {
                _logger.LogWithCategory(LogLevel.Warning, LogCategory.API,
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