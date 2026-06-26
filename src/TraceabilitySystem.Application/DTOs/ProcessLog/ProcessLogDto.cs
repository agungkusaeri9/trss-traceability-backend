using System;
using System.Collections.Generic;

namespace TraceabilitySystem.Application.DTOs.ProcessLog;

public class ProcessLogDto
{
    public long Id { get; set; }
    public string SerialNumberCode { get; set; } = string.Empty;
    public string? PartNumber { get; set; }
    public string? PartName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    public List<ProcessGroupDto> Details { get; set; } = new();
}

public class ProcessGroupDto
{
    public string? ProcessName { get; set; }
    public List<ProcessParameterValueDto> Parameters { get; set; } = new();
}

public class ProcessParameterValueDto
{
    public int ParameterId { get; set; }
    public string? ParameterName { get; set; }
    public string? DataType { get; set; }
    public List<object?> Values { get; set; } = new();
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
}
