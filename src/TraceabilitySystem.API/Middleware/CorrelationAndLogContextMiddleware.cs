using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Serilog.Context;
using TraceabilitySystem.Shared.Constants;

namespace TraceabilitySystem.API.Middleware;

public class CorrelationAndLogContextMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-ID";
    private readonly RequestDelegate _next;

    public CorrelationAndLogContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 1. Resolve or generate CorrelationId
        string correlationId;
        if (context.Request.Headers.TryGetValue(CorrelationIdHeader, out var existingCorrelationId) &&
            !string.IsNullOrWhiteSpace(existingCorrelationId))
        {
            correlationId = existingCorrelationId.ToString();
        }
        else
        {
            correlationId = Guid.NewGuid().ToString("N")[..12];
        }

        // 2. Set CorrelationId to response headers
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationIdHeader] = correlationId;
            return Task.CompletedTask;
        });

        // 3. Resolve RequestId & UserId
        string requestId = context.TraceIdentifier;
        string userId = GetUserId(context);

        // 4. Push properties to Serilog LogContext for the duration of the HTTP request
        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("RequestId", requestId))
        using (LogContext.PushProperty("UserId", userId))
        using (LogContext.PushProperty("Category", LogCategory.API))
        {
            await _next(context);
        }
    }

    private static string GetUserId(HttpContext context)
    {
        var user = context.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            return "Anonymous";
        }

        return user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst("sub")?.Value
            ?? user.FindFirst(ClaimTypes.Name)?.Value
            ?? user.Identity?.Name
            ?? "Authenticated";
    }
}
