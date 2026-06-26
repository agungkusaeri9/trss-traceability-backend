using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mapster;
using TraceabilitySystem.Application.DTOs.Dashboard;
using TraceabilitySystem.Application.DTOs.ProcessLog;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Domain.Entities;
using TraceabilitySystem.Domain.Interfaces;

namespace TraceabilitySystem.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IProcessLogRepository _processLogRepository;
    private readonly IPartRepository _partRepository;
    private readonly IIssueRepository _issueRepository;
    private readonly IProcessLogService _processLogService;
    private readonly ITraceabilitySummarySimulator _traceabilitySummarySimulator;
    private readonly ISerialNumberRepository _serialNumberRepository;

    public DashboardService(
        IProcessLogRepository processLogRepository,
        IPartRepository partRepository,
        IIssueRepository issueRepository,
        IProcessLogService processLogService,
        ITraceabilitySummarySimulator traceabilitySummarySimulator,
        ISerialNumberRepository serialNumberRepository)
    {
        _processLogRepository = processLogRepository;
        _partRepository = partRepository;
        _issueRepository = issueRepository;
        _processLogService = processLogService;
        _traceabilitySummarySimulator = traceabilitySummarySimulator;
        _serialNumberRepository = serialNumberRepository;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.Now;
        var todayStart = now.Date;
        var monthStart = new DateTime(now.Year, now.Month, 1);

        var summary = new DashboardSummaryDto();

        summary.Today.TotalProduction = await _processLogRepository.CountAsync(x => x.CreatedAt >= todayStart, cancellationToken);
        summary.Today.OkCount = await _processLogRepository.CountAsync(x => x.CreatedAt >= todayStart && x.IsActive, cancellationToken);
        summary.Today.NgCount = summary.Today.TotalProduction - summary.Today.OkCount;
        summary.Today.YieldRate = CalculateYield(summary.Today.TotalProduction, summary.Today.OkCount);

        summary.ThisMonth.TotalProduction = await _processLogRepository.CountAsync(x => x.CreatedAt >= monthStart, cancellationToken);
        summary.ThisMonth.OkCount = await _processLogRepository.CountAsync(x => x.CreatedAt >= monthStart && x.IsActive, cancellationToken);
        summary.ThisMonth.NgCount = summary.ThisMonth.TotalProduction - summary.ThisMonth.OkCount;
        summary.ThisMonth.YieldRate = CalculateYield(summary.ThisMonth.TotalProduction, summary.ThisMonth.OkCount);

        summary.Total.TotalProduction = await _processLogRepository.CountAsync(null, cancellationToken);
        summary.Total.OkCount = await _processLogRepository.CountAsync(x => x.IsActive, cancellationToken);
        summary.Total.NgCount = summary.Total.TotalProduction - summary.Total.OkCount;
        summary.Total.YieldRate = CalculateYield(summary.Total.TotalProduction, summary.Total.OkCount);

        return summary;
    }

    public Task<List<DashboardSummaryFieldDto>> GetTraceabilitySummaryAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_traceabilitySummarySimulator.GetSnapshot());
    }

    public async Task<DashboardStatsDto> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        var stats = new DashboardStatsDto();

        // 1. Quality Distribution (Pie Chart)
        int ok = await _processLogRepository.CountAsync(x => x.IsActive, cancellationToken);
        int total = await _processLogRepository.CountAsync(null, cancellationToken);
        int ng = total - ok;

        stats.QualityDistribution.Add(new ChartDataDto { Label = "OK", Value = ok });
        stats.QualityDistribution.Add(new ChartDataDto { Label = "NG", Value = ng });

        // 2. Top Parts Production (Bar Chart)
        // Note: In a real app, we'd use a more optimized query or Dapper. 
        // For now, we'll use a simple group by on the logs we fetch.
        var allLogs = await _processLogRepository.GetAllAsync(cancellationToken);
        
        // This is a bit heavy, but for seed data it's fine. 
        // In production, you'd want a specific repository method for this.
        var topParts = new List<ChartDataDto>();
        var partCount = new Dictionary<string, int>();

        foreach (var log in allLogs.Take(100))
        {
            var serialNumber = await _serialNumberRepository.GetWithRelatedAsync(log.SerialNumberId, cancellationToken);
            if (serialNumber != null && serialNumber.Issues.Any())
            {
                var firstIssue = serialNumber.Issues.FirstOrDefault();
                if (firstIssue?.Issue?.StockIn?.Part != null)
                {
                    string partNumber = firstIssue.Issue.StockIn.Part.Number;
                    if (partCount.ContainsKey(partNumber))
                    {
                        partCount[partNumber]++;
                    }
                    else
                    {
                        partCount[partNumber] = 1;
                    }
                }
            }
        }

        topParts = partCount
            .Select(kvp => new ChartDataDto { Label = kvp.Key, Value = kvp.Value })
            .OrderByDescending(x => x.Value)
            .Take(5)
            .ToList();

        stats.TopPartsProduction = topParts;

        // 3. Production Trend (Last 7 Days)
        for (int i = 6; i >= 0; i--)
        {
            var date = DateTime.Now.Date.AddDays(-i);
            var nextDate = date.AddDays(1);
            var count = await _processLogRepository.CountAsync(x => x.CreatedAt >= date && x.CreatedAt < nextDate, cancellationToken);
            
            stats.ProductionTrend.Add(new ChartDataDto 
            { 
                Label = date.ToString("dd MMM"), 
                Value = count 
            });
        }

        return stats;
    }

    public async Task<List<ProcessLogDto>> GetRecentLogsAsync(int count = 5, CancellationToken cancellationToken = default)
    {
        var paged = await _processLogRepository.GetPagedLogsAsync(1, count, cancellationToken: cancellationToken);
        
        var dtos = new List<ProcessLogDto>();
        foreach(var log in paged.Items)
        {
            dtos.Add(await _processLogService.GetProcessLogByIdAsync(log.Id, cancellationToken));
        }
        
        return dtos;
    }

    private double CalculateYield(int total, int ok)
    {
        if (total == 0) return 0;
        return Math.Round((double)ok / total * 100, 2);
    }
}
