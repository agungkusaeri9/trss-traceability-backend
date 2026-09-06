using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TraceabilitySystem.Application.DTOs.ProcessLog;

public class ProcessLogMockDto
{
    [JsonPropertyName("Id")]
    public long Id { get; set; }

    [JsonPropertyName("Timestamp")]
    public string Timestamp { get; set; } = string.Empty;

    [JsonPropertyName("SerialNumberClinching")]
    public string SerialNumberClinching { get; set; } = string.Empty;

    [JsonPropertyName("SerialNumberMFan")]
    public string? SerialNumberMFan { get; set; }

    [JsonPropertyName("SerialNumber")]
    public string SerialNumber
    {
        get => !string.IsNullOrEmpty(SerialNumberClinching) ? SerialNumberClinching : string.Empty;
        set
        {
            if (string.IsNullOrEmpty(SerialNumberClinching))
                SerialNumberClinching = value;
        }
    }

    // 1. Clinching Short Side
    [JsonPropertyName("CoreAsmValue")]
    public string CoreAsmValue { get; set; } = string.Empty;

    [JsonPropertyName("UpperTankAsmValue")]
    public string UpperTankAsmValue { get; set; } = string.Empty;

    [JsonPropertyName("LowerTankAsmValue")]
    public string LowerTankAsmValue { get; set; } = string.Empty;

    [JsonPropertyName("ORingSetResult")]
    public bool ORingSetResult { get; set; } = true;

    [JsonPropertyName("NgBoxSensorShortSideValue")]
    public string NgBoxSensorShortSideValue { get; set; } = "ON";

    // 2. Clinching Long Side
    [JsonPropertyName("ClinchingHeightValues")]
    [JsonConverter(typeof(InlineDoubleArrayConverter))]
    public double[] ClinchingHeightValues { get; set; } = Array.Empty<double>();

    private double? _clinchingHeightAverage;

    [JsonPropertyName("ClinchingHeightAverage")]
    public double ClinchingHeightAverage
    {
        get => _clinchingHeightAverage ?? (ClinchingHeightValues != null && ClinchingHeightValues.Length > 0
            ? Math.Round(ClinchingHeightValues.Average(), 2)
            : 0.0);
        set => _clinchingHeightAverage = value;
    }

    [JsonPropertyName("EndPlateWidthResults")]
    [JsonConverter(typeof(InlineBoolArrayConverter))]
    public bool[] EndPlateWidthResults { get; set; } = Array.Empty<bool>();

    private bool? _endPlateWidthStatus;

    [JsonPropertyName("EndPlateWidthStatus")]
    public bool EndPlateWidthStatus
    {
        get => _endPlateWidthStatus ?? (EndPlateWidthResults != null && EndPlateWidthResults.Length > 0 && EndPlateWidthResults.All(x => x));
        set => _endPlateWidthStatus = value;
    }

    [JsonPropertyName("NgBoxSensorLongSideValue")]
    public string NgBoxSensorLongSideValue { get; set; } = "ON";

    // 3. M-Fan Assy
    [JsonPropertyName("LotFanAsmResult")]
    public string? LotFanAsmResult { get; set; }

    [JsonPropertyName("LotMotorAsmResult")]
    public string? LotMotorAsmResult { get; set; }

    [JsonPropertyName("LotGuideAsmResult")]
    public string? LotGuideAsmResult { get; set; }

    [JsonPropertyName("BoltTightenValue")]
    public string? BoltTightenValue { get; set; }

    [JsonPropertyName("BoltTightenQtyValue")]
    public string? BoltTightenQtyValue { get; set; }

    [JsonPropertyName("NutTightenValue")]
    public bool? NutTightenValue { get; set; }

    // 4. M-Fan Inspection
    [JsonPropertyName("MFanInspectionRotationSpeedMaxValue")]
    public double? MFanInspectionRotationSpeedMaxValue { get; set; }

    [JsonPropertyName("MFanInspectionRotationSpeedMinValue")]
    public double? MFanInspectionRotationSpeedMinValue { get; set; }

    [JsonPropertyName("MFanInspectionAmpereMaxValue")]
    public double? MFanInspectionAmpereMaxValue { get; set; }

    [JsonPropertyName("MFanInspectionAmpereMinValue")]
    public double? MFanInspectionAmpereMinValue { get; set; }

    [JsonPropertyName("MFanInspectionWindDirectionValue")]
    public string? MFanInspectionWindDirectionValue { get; set; }

    [JsonPropertyName("MFanTestResult")]
    public bool? MFanTestResult { get; set; }

    [JsonPropertyName("NgBoxSensorMFanInspectionValue")]
    public string? NgBoxSensorMFanInspectionValue { get; set; }

    // 5. ECM Assy
    [JsonPropertyName("RadCoreAsmNameLabelResult")]
    public bool? RadCoreAsmNameLabelResult { get; set; }

    [JsonPropertyName("MotorFanAssyLabelResult")]
    public bool? MotorFanAssyLabelResult { get; set; }

    [JsonPropertyName("EcmAssyBoltTightenValue")]
    public double? EcmAssyBoltTightenValue { get; set; }

    [JsonPropertyName("EcmAssyBoltTightenQtyValue")]
    public double? EcmAssyBoltTightenQtyValue { get; set; }

    [JsonPropertyName("NgBoxSensorEcmAssyValue")]
    public string? NgBoxSensorEcmAssyValue { get; set; }

    // 6. Final Inspection
    [JsonPropertyName("FinalInspectionRadCoreAsmNameLabelResult")]
    public bool? FinalInspectionRadCoreAsmNameLabelResult { get; set; }

    [JsonPropertyName("CheckPoints")]
    [JsonConverter(typeof(InlineBoolArrayConverter))]
    public bool[]? CheckPoints { get; set; }

    private bool? _checkPointStatus;

    [JsonPropertyName("CheckPointStatus")]
    public bool? CheckPointStatus
    {
        get => _checkPointStatus ?? (CheckPoints != null && CheckPoints.Length > 0 ? CheckPoints.All(x => x) : null);
        set => _checkPointStatus = value;
    }

    [JsonPropertyName("NgBoxSensorFinalInspectionValue")]
    public string? NgBoxSensorFinalInspectionValue { get; set; }

    // Overall
    [JsonPropertyName("OverallStatus")]
    public string OverallStatus { get; set; } = "PASSED";
}

/// <summary>
/// Serializes double array to a single-line horizontal string like [12.44, 12.45, 12.46]
/// </summary>
public class InlineDoubleArrayConverter : JsonConverter<double[]>
{
    public override double[]? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var list = new List<double>();
        if (reader.TokenType != JsonTokenType.StartArray) return list.ToArray();
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                list.Add(reader.GetDouble());
            }
        }
        return list.ToArray();
    }

    public override void Write(Utf8JsonWriter writer, double[] value, JsonSerializerOptions options)
    {
        if (value == null || value.Length == 0)
        {
            writer.WriteRawValue("[]");
            return;
        }

        var formatted = string.Join(", ", value.Select(v => v.ToString("0.##", CultureInfo.InvariantCulture)));
        writer.WriteRawValue($"[{formatted}]");
    }
}

/// <summary>
/// Serializes boolean array to a single-line horizontal string like [true, true, true]
/// </summary>
public class InlineBoolArrayConverter : JsonConverter<bool[]>
{
    public override bool[]? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null) return null;
        var list = new List<bool>();
        if (reader.TokenType != JsonTokenType.StartArray) return list.ToArray();
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            if (reader.TokenType == JsonTokenType.True || reader.TokenType == JsonTokenType.False)
            {
                list.Add(reader.GetBoolean());
            }
        }
        return list.ToArray();
    }

    public override void Write(Utf8JsonWriter writer, bool[]? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }
        if (value.Length == 0)
        {
            writer.WriteRawValue("[]");
            return;
        }

        var formatted = string.Join(", ", value.Select(v => v ? "true" : "false"));
        writer.WriteRawValue($"[{formatted}]");
    }
}
