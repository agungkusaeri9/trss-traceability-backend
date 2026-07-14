using System;
using System.Collections.Generic;

namespace TraceabilitySystem.Application.DTOs.ProcessLog;

/// <summary>Process log detail — format flat sesuai format response baru.</summary>
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
    public bool IsParent { get; set; }
    public bool Status { get; set; }
    public string SerialNumberCode { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public List<IssueSummaryDto> Issues { get; set; } = new();
    public List<ProcessGroupDto> Processes { get; set; } = new();
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
