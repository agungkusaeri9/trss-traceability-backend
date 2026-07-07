using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TraceabilitySystem.Application.DTOs.ProcessLog;

public class CreateProcessLogRequestDto
{
    [JsonIgnore]
    public string MessageId { get; set; } = Guid.CreateVersion7().ToString();

    [JsonPropertyName("serial_number_clinching")]
    public string SerialNumberClinching { get; set; } = string.Empty;

     [JsonPropertyName("serial_number_m_fan_assy")]
    public string SerialNumberMFanAssy { get; set; } = string.Empty;

     [JsonPropertyName("serial_number")]
    public string? SerialNumber { get; set; } = string.Empty;

    [JsonPropertyName("process_code")]
    public string ProcessCode { get; set; } = string.Empty;

    [JsonPropertyName("operator_username")]
    public string? OperatorUsername { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime? Timestamp { get; set; }

    [JsonPropertyName("isOk")]
    public bool? IsOk { get; set; }

    [JsonPropertyName("data")]
    public Dictionary<string, object>? Data { get; set; }

    [JsonIgnore]
    public bool IsFInihed { get; set; } = false;


}

