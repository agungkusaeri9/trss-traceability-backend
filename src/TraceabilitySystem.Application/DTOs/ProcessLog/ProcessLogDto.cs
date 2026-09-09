using System;
using System.Collections.Generic;
using TraceabilitySystem.Application.DTOs.StockIn;

namespace TraceabilitySystem.Application.DTOs.ProcessLog;

/// <summary>Process log detail — format flat sesuai format response baru.</summary>
/// 
public class ProcessLogDto
{
    public long Id { get; set; }
    public bool IsActive { get; set; }
    public bool Status { get; set; }
    public bool IsParent { get; set; }
    public string SerialNumberCode { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public List<IssueSummaryDto> Issues { get; set; } = new();
    public List<ProcessGroupDto> Processes { get; set; } = new();
}

public class ProcessLogByConceptDto
{
    public long Id { get; set; }
    public ProcessLogByConceptParentDto? ParentDetail { get; set; }
    public ProcessLogByConceptChildDto? ChildDetail { get; set; }
    public bool Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ProcessLogByConceptParentDto
{
    public string SerialNumberCode { get; set; } = string.Empty;
    public string ProcessName { get; set; } = string.Empty;
    public string ParameterName { get; set; } = string.Empty;
    public string? IssueNumber { get; set; }
    public bool Status { get; set; } = false;


}

public class ProcessLogByConceptChildDto
{
    public string SerialNumberCode { get; set; } = string.Empty;
    public string ProcessName { get; set; } = string.Empty;
    public string ParameterName { get; set; } = string.Empty;
    public string? IssueNumber { get; set; }
    public bool Status { get; set; } = false;
}

/// <summary>Process log list — format flat sesuai format response baru.</summary>
public class ProcessLogListDto
{
    public long Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string SerialNumberCode { get; set; } = string.Empty;
    public bool Status { get; set; }
    public bool IsFinished { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public object? Detail { get; set; }
}

public class ProcessLogClinchingDetailDto
{
    public string SerialNumberClinching { get; set; } = string.Empty;
    public string CoreAsmValue { get; set; } = string.Empty;
    public string UpperTankAsmValue { get; set; } = string.Empty;
    public string LowerTankAsmValue { get; set; } = string.Empty;
    public int? ORingSetResult { get; set; }
    public string NgBoxSensorShortSideValue { get; set; } = "ON";
    public double[] ClinchingHeightValues { get; set; } = Array.Empty<double>();
    public bool? ClinchingHeightStatus { get; set; }
    public int[] EndPlateWidthResults { get; set; } = Array.Empty<int>();
    public bool? EndPlateWidthStatus { get; set; }
    public string NgBoxSensorLongSideValue { get; set; } = "ON";
    public int? CapTypePositionResult { get; set; }
    public int? LeakResult { get; set; }
    public double? LeakLastLeakageValue { get; set; }
}

public class ProcessLogMFanDetailDto
{
    public string? SerialNumberMFan { get; set; }
    public string? LotFanAsmResult { get; set; }
    public string? LotMotorAsmResult { get; set; }
    public string? LotGuideAsmResult { get; set; }
    public string? BoltTightenValue { get; set; }
    public string? BoltTightenQtyValue { get; set; }
    public string? NutTightenValue { get; set; }
    public double? MFanInspectionRotationSpeedMaxValue { get; set; }
    public double? MFanInspectionRotationSpeedMinValue { get; set; }
    public double? MFanInspectionAmpereMaxValue { get; set; }
    public double? MFanInspectionAmpereMinValue { get; set; }
    public string? MFanInspectionWindDirectionValue { get; set; }
    public int? MFanTestResult { get; set; }
    public string? NgBoxSensorMFanInspectionValue { get; set; } = "ON";
}


    /// <summary>Summary informasi issue (lot) beserta part yang terkait.</summary>
    public class IssueSummaryDto
    {
        public string IssueType { get; set; } = string.Empty;
        public string IssueNumber { get; set; } = string.Empty;
        public string PartNumber { get; set; } = string.Empty;
        public string PartName { get; set; } = string.Empty;
    }

    public class ProcessGroupDto
    {
        public string? ProcessCode { get; set; }
        public string? ProcessName { get; set; }
        public bool Result { get; set; }
        public List<ProcessParameterValueDto> Parameters { get; set; } = new();
    }

    public class ProcessParameterValueDto
    {
        public string? ParameterCode { get; set; }
        public string? ParameterName { get; set; }
        public object? Value { get; set; }
        public bool Status { get; set; }
    }

    public class ProcessLogDetailDto
    {
        public long Id { get; set; }
        public int ProcessId { get; set; }
        public string? ProcessName { get; set; }
        public int ParameterId { get; set; }
        public string? ParameterName { get; set; }
        public string? DataType { get; set; }

        public decimal? ValueNumber { get; set; }
        public string? ValueText { get; set; }
        public bool? ValueBoolean { get; set; }

        public string? DisplayValue { get; set; }
        public bool Status { get; set; }
    }

    public class ProcessLogFullValueDto
    {
        public long Id { get; set; }

        public string SerialNumberCode { get; set; } = string.Empty;

        public ProcessLogFullValueParentDto Clinching { get; set; } = new();

        public ProcessLogFullValueChildDto MFan { get; set; } = new();

        public List<ProcessLogFullValueDetailDto> Overall { get; set; } = new();

        public bool Status { get; set; }
        public bool IsFinished { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }

    public class ProcessLogFullValueParentDto
    {
        public string SerialNumberCode { get; set; } = string.Empty;

        public List<ProcessLogFullValueDetailDto> Details { get; set; } = new();
    }

    public class ProcessLogFullValueChildDto
    {
        public string SerialNumberCode { get; set; } = string.Empty;

        public List<ProcessLogFullValueDetailDto> Details { get; set; } = new();
    }

    public class ProcessLogFullValueDetailDto
    {
        public string ProcessCode { get; set; } = string.Empty;

        public string ProcessName { get; set; } = string.Empty;

        public string ParameterCode { get; set; } = string.Empty;

        public string ParameterName { get; set; } = string.Empty;

        public string? Value { get; set; }
    }