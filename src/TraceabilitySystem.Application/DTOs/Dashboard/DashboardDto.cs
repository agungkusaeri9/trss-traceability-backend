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
    public double YieldRate { get; set; } // Percentage of OK
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
