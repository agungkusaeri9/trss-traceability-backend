namespace TraceabilitySystem.Application.DTOs.MqttPrintRequest;

public class CreateMqttPrintRequestDto
{
    public string ProcessCode { get; set; } = string.Empty;

    public string IssueNumber { get; set; } = string.Empty;

    public string RawPayload { get; set; } = string.Empty;
}
