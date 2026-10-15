using System.Text.Json.Serialization;

namespace TraceabilitySystem.Application.DTOs.Process;

public class ProcessIsPublisherMqttDto
{
    [JsonPropertyName("process_code")]
    public string? ProcessCode { get; set; }

    [JsonPropertyName("isMqttPublisher")]
    public bool? IsMqttPublisher { get; set; }

    [JsonPropertyName("isMqttPublihser")]
    public bool? IsMqttPublihser
    {
        get => IsMqttPublisher;
        set
        {
            if (value.HasValue) IsMqttPublisher = value;
        }
    }

    [JsonPropertyName("isMqttPuslihser")]
    public bool? IsMqttPuslihser
    {
        get => IsMqttPublisher;
        set
        {
            if (value.HasValue) IsMqttPublisher = value;
        }
    }

    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; set; }
}
