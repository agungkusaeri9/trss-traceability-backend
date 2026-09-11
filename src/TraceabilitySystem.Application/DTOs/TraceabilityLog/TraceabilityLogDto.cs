using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

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
    public List<TraceabilityLogParameterDto> Clinching { get; set; } = new();

    [JsonPropertyName("mfan")]
    public List<TraceabilityLogParameterDto> MFan { get; set; } = new();

    [JsonPropertyName("ecm")]
    public List<TraceabilityLogParameterDto> Ecm { get; set; } = new();

    [JsonPropertyName("final")]
    public List<TraceabilityLogParameterDto> Final { get; set; } = new();

    [JsonIgnore]
    public List<TraceabilityLogParameterDto> ClinchingProcess => Clinching;

    [JsonIgnore]
    public List<TraceabilityLogParameterDto> MFanProcess => MFan;

    [JsonIgnore]
    public List<TraceabilityLogParameterDto> EcmAssy => Ecm;

    [JsonIgnore]
    public List<TraceabilityLogParameterDto> FinalInspection => Final;
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
