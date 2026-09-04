using System;

namespace TraceabilitySystem.Application.DTOs.MqttLog;

public class MqttMessageLogDto
{
    public ulong Id { get; set; }
    public string MessageId { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string? ProcessName { get; set; }
    public string? OperatorUsername { get; set; }
    public bool? IsOk { get; set; }
    public string? Payload { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public DateTime ReceivedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
