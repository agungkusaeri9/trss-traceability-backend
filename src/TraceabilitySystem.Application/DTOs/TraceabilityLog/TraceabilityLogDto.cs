using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using TraceabilitySystem.Application.DTOs.ProcessLog;

namespace TraceabilitySystem.Application.DTOs.TraceabilityLog;

public class TraceabilityLogDto
{
    public long Id { get; set; }

    [JsonIgnore]
    public string? Code { get; set; }

    public string? SerialNumberClinching { get; set; }
    public string? SerialNumberMFan { get; set; }
    public bool Status { get; set; }
    public bool IsFinish { get; set; }
    

    [JsonPropertyName("issueNumbersClinching")]
    public List<string> IssueNumbersClinching { get; set; } = new();

    [JsonPropertyName("issueNumbersMfan")]
    public List<string> IssueNumbersMfan { get; set; } = new();

    [JsonPropertyName("detail")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TraceabilityLogProcessGroupDto? Detail { get; set; }

    [JsonIgnore]
    public TraceabilityLogProcessGroupDto? Details => Detail;

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class TraceabilityLogProcessGroupDto
{
    [JsonPropertyName("clinching")]
    public List<TraceabilityLogProcessSectionDto> Clinching { get; set; } = new();

    [JsonPropertyName("heLeak")]
    public List<TraceabilityLogProcessSectionDto> HeLeak { get; set; } = new();

    [JsonPropertyName("mfan")]
    public List<TraceabilityLogProcessSectionDto> MFan { get; set; } = new();

    [JsonPropertyName("ecm")]
    public List<TraceabilityLogProcessSectionDto> Ecm { get; set; } = new();

    [JsonPropertyName("final")]
    public List<TraceabilityLogProcessSectionDto> Final { get; set; } = new();

    [JsonIgnore]
    public List<TraceabilityLogParameterDto> ClinchingProcess => Clinching.SelectMany(c => c.Parameters).ToList();

    [JsonIgnore]
    public List<TraceabilityLogParameterDto> MFanProcess => MFan.SelectMany(m => m.Parameters).ToList();

    [JsonIgnore]
    public List<TraceabilityLogParameterDto> EcmAssy => Ecm.SelectMany(e => e.Parameters).ToList();

    [JsonIgnore]
    public List<TraceabilityLogParameterDto> FinalInspection => Final.SelectMany(f => f.Parameters).ToList();
}

public class TraceabilityLogProcessSectionDto
{
    [JsonPropertyName("processCode")]
    public string? ProcessCode { get; set; }

    [JsonPropertyName("processName")]
    public string? ProcessName { get; set; }

    [JsonPropertyName("result")]
    public bool Result { get; set; } = true;

    [JsonPropertyName("isMqttPublisher")]
    public bool IsMqttPublisher { get; set; } = true;

    [JsonPropertyName("status")]
    public string? Status => MqttPublisherStatus;

    [JsonPropertyName("mqttPublisherStatus")]
    public string? MqttPublisherStatus { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonPropertyName("parameters")]
    public List<TraceabilityLogParameterDto> Parameters { get; set; } = new();

    [JsonPropertyName("history")]
    public List<ProcessHistoryItemDto> History { get; set; } = new();
}

public class TraceabilityLogParameterDto
{
    [JsonPropertyName("parameter")]
    public string? Parameter { get; set; }

    [JsonPropertyName("parameterDesc")]
    public string? ParameterDesc { get; set; }

    [JsonPropertyName("value")]
    public object? Value { get; set; }

    [JsonPropertyName("status")]
    public bool? Status { get; set; }
}
