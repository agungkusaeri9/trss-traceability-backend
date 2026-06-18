namespace TraceabilitySystem.Application.DTOs.MqttPrintRequest;

public class MqttPrintRequestDto
{
    public long Id { get; set; }

    public string ProcessCode { get; set; } = string.Empty;

    public string IssueNumber { get; set; } = string.Empty;

    public string RawPayload { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
