using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using TraceabilitySystem.Shared.Constants;

namespace TraceabilitySystem.Shared.Helpers;

public static class LoggerExtensions
{
    public static IDisposable? BeginCategoryScope(this ILogger logger, string category)
    {
        return logger.BeginScope(new Dictionary<string, object>
        {
            ["Category"] = category
        });
    }

    public static void LogWithCategory(this ILogger logger, LogLevel level, string category, string message, params object?[] args)
    {
        using (logger.BeginCategoryScope(category))
        {
            logger.Log(level, message, args);
        }
    }

    public static void LogWithCategory(this ILogger logger, LogLevel level, string category, Exception? exception, string message, params object?[] args)
    {
        using (logger.BeginCategoryScope(category))
        {
            logger.Log(level, exception, message, args);
        }
    }

    public static void LogApi(this ILogger logger, LogLevel level, string message, params object?[] args) =>
        logger.LogWithCategory(level, LogCategory.API, message, args);

    public static void LogApp(this ILogger logger, LogLevel level, string message, params object?[] args) =>
        logger.LogWithCategory(level, LogCategory.Application, message, args);

    public static void LogAuth(this ILogger logger, LogLevel level, string message, params object?[] args) =>
        logger.LogWithCategory(level, LogCategory.Authentication, message, args);

    public static void LogAuth(this ILogger logger, LogLevel level, Exception? exception, string message, params object?[] args) =>
        logger.LogWithCategory(level, LogCategory.Authentication, exception, message, args);

    public static void LogSecurity(this ILogger logger, LogLevel level, string message, params object?[] args) =>
        logger.LogWithCategory(level, LogCategory.Security, message, args);

    public static void LogSecurity(this ILogger logger, LogLevel level, Exception? exception, string message, params object?[] args) =>
        logger.LogWithCategory(level, LogCategory.Security, exception, message, args);

    public static void LogAudit(this ILogger logger, string action, string message, params object?[] args)
    {
        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["Category"] = LogCategory.Audit,
            ["AuditAction"] = action
        }))
        {
            logger.LogInformation(message, args);
        }
    }

    public static void LogIntegration(this ILogger logger, LogLevel level, string message, params object?[] args) =>
        logger.LogWithCategory(level, LogCategory.Integration, message, args);

    public static void LogDatabase(this ILogger logger, LogLevel level, string message, params object?[] args) =>
        logger.LogWithCategory(level, LogCategory.Database, message, args);
}
