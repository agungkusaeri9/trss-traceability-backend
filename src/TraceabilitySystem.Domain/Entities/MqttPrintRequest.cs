using System;

namespace TraceabilitySystem.Domain.Entities;

public class MqttPrintRequest
{
    public long Id { get; set; }

    public string ProcessCode { get; set; } = string.Empty;

    public string IssueNumber { get; set; } = string.Empty;

    public string RawPayload { get; set; } = string.Empty;

    public string Status { get; set; } = "Pending"; // Pending, Processed, Failed

    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ProcessedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
