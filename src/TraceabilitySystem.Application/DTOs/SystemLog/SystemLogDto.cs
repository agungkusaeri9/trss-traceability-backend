using System;

namespace TraceabilitySystem.Application.DTOs.SystemLog;

public class SystemLogDto
{
    public string Id { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
    public string Level { get; set; } = string.Empty;
    public string Service { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public string? RequestId { get; set; }
    public string? UserId { get; set; }
    public string Message { get; set; } = string.Empty;
    public string SourceFile { get; set; } = string.Empty;
}
