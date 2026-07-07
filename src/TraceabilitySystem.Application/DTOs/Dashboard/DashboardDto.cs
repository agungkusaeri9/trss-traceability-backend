using System;
using System.Collections.Generic;

namespace TraceabilitySystem.Application.DTOs.Dashboard;

public class DashboardSummaryDto
{
    public ProductionCountDto Today { get; set; } = new();
    public ProductionCountDto ThisMonth { get; set; } = new();
    public ProductionCountDto Total { get; set; } = new();
}

public class ProductionCountDto
{
    public int TotalProduction { get; set; }
    public int OkCount { get; set; }
    public int NgCount { get; set; }
    public double YieldRate { get; set; }
}

public class DashboardSummaryFieldDto
{
    public int Order { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public object? Value { get; set; }
}

public class DashboardStatsDto
{
    public List<ChartDataDto> QualityDistribution { get; set; } = new(); // Pie Chart: OK vs NG
    public List<ChartDataDto> TopPartsProduction { get; set; } = new(); // Bar Chart: Parts
    public List<ChartDataDto> ProductionTrend { get; set; } = new(); // Line Chart: Last 7 Days
}

public class ChartDataDto
{
    public string Label { get; set; } = string.Empty;
    public double Value { get; set; }
    public string? ExtraInfo { get; set; }
}

// Traceability Flow DTOs
public class TraceabilityFlowDto
{
    public List<SerialNumberItemDto> Queue { get; set; } = new();
    public List<StationDto> Stations { get; set; } = new();
    public List<SerialNumberItemDto> OkList { get; set; } = new();
    public List<SerialNumberItemDto> NgList { get; set; } = new();
    public FlowStatsDto Stats { get; set; } = new();
}

public class SerialNumberItemDto
{
    public int Id { get; set; }
    public string SerialNumberCode { get; set; } = string.Empty;
    public string? CurrentProcess { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class StationDto
{
    public int Order { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<SerialNumberItemDto> Items { get; set; } = new();
}

public class FlowStatsDto
{
    public int QueueCount { get; set; }
    public int InProgressCount { get; set; }
    public int OkCount { get; set; }
    public int NgCount { get; set; }
    public DateTime LastUpdated { get; set; }
}
