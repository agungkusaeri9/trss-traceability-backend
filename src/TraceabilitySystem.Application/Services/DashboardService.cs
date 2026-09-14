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
using TraceabilitySystem.Shared.Helpers;

namespace TraceabilitySystem.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IProcessLogRepository _processLogRepository;
    private readonly IPartRepository _partRepository;
    private readonly IIssueRepository _issueRepository;
    private readonly IProcessLogService _processLogService;
    private readonly ITraceabilitySummarySimulator _traceabilitySummarySimulator;
    private readonly ISerialNumberRepository _serialNumberRepository;
    private readonly IProcessRepository _processRepository;
    private readonly ITraceabilityLogService _traceabilityLogService;
    private readonly ITraceabilityLogRepository _traceabilityLogRepository;

    // Process order definitions for traceability flow
    private static readonly string[] ProcessOrder = new[]
    {
        "CLINCHING_SHORT_SIDE",
        "CLINCHING_LONG_SIDE",
        "HE_LEAK",
        "M_FAN_ASSY",
        "M_FAN_INSPECTION",
        "ECM_ASSY",
        "FINAL_INSPECTION"
    };

    public DashboardService(
        IProcessLogRepository processLogRepository,
        IPartRepository partRepository,
        IIssueRepository issueRepository,
        IProcessLogService processLogService,
        ITraceabilitySummarySimulator traceabilitySummarySimulator,
        ISerialNumberRepository serialNumberRepository,
        IProcessRepository processRepository,
        ITraceabilityLogService traceabilityLogService,
        ITraceabilityLogRepository traceabilityLogRepository)
    {
        _processLogRepository = processLogRepository;
        _partRepository = partRepository;
        _issueRepository = issueRepository;
        _processLogService = processLogService;
        _traceabilitySummarySimulator = traceabilitySummarySimulator;
        _serialNumberRepository = serialNumberRepository;
        _processRepository = processRepository;
        _traceabilityLogService = traceabilityLogService;
        _traceabilityLogRepository = traceabilityLogRepository;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var nowJakarta = DateTimeHelper.GetJakartaNow();
        var todayStartJakarta = nowJakarta.Date;
        var monthStartJakarta = new DateTime(nowJakarta.Year, nowJakarta.Month, 1);
        var yearStartJakarta = new DateTime(nowJakarta.Year, 1, 1);

        var summary = new DashboardSummaryDto();

        // Query directly from TraceabilityLogs table using UTC range corresponding to Jakarta year start to now
        var yearStartUtc = yearStartJakarta.ToUtcFromJakarta();
        var endDateUtc = nowJakarta.AddDays(1).Date.ToUtcFromJakarta();

        var yearResult = await _traceabilityLogRepository.GetAllTraceabilityNewAsync(
            page: 1, pageSize: int.MaxValue,
            startDate: yearStartUtc, endDate: endDateUtc,
            cancellationToken: cancellationToken);

        var allLogs = yearResult.Items.ToList();

        // Helper: count only finished logs (IsFinish == true) by Jakarta CreatedAt range and Status.
        static (int total, int ok, int ng) CountRange(
            IEnumerable<TraceabilityLog> logs, DateTime fromJakarta, DateTime toJakarta)
        {
            var inRange = logs.Where(l =>
            {
                if (!l.IsFinish) return false;
                var localCreatedAt = l.CreatedAt.ToJakartaTime();
                return localCreatedAt >= fromJakarta && localCreatedAt < toJakarta;
            }).ToList();
            int ok = inRange.Count(l => l.Status);
            int ng = inRange.Count(l => !l.Status);
            return (inRange.Count, ok, ng);
        }

        var (tTotal, tOk, tNg) = CountRange(allLogs, todayStartJakarta, todayStartJakarta.AddDays(1));
        summary.Today.TotalProduction = tTotal;
        summary.Today.OkCount = tOk;
        summary.Today.NgCount = tNg;
        summary.Today.YieldRate = CalculateYield(tTotal, tOk);

        var (mTotal, mOk, mNg) = CountRange(allLogs, monthStartJakarta,
            nowJakarta.Month == 12 ? new DateTime(nowJakarta.Year + 1, 1, 1) : monthStartJakarta.AddMonths(1));
        summary.ThisMonth.TotalProduction = mTotal;
        summary.ThisMonth.OkCount = mOk;
        summary.ThisMonth.NgCount = mNg;
        summary.ThisMonth.YieldRate = CalculateYield(mTotal, mOk);

        var (yTotal, yOk, yNg) = CountRange(allLogs, yearStartJakarta, new DateTime(nowJakarta.Year + 1, 1, 1));
        summary.Total.TotalProduction = yTotal;
        summary.Total.OkCount = yOk;
        summary.Total.NgCount = yNg;
        summary.Total.YieldRate = CalculateYield(yTotal, yOk);

        return summary;
    }

    public Task<List<DashboardSummaryFieldDto>> GetTraceabilitySummaryAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_traceabilitySummarySimulator.GetSnapshot());
    }

    public async Task<DashboardStatsDto> GetStatsAsync(int topPart, int trendDays, string? period = null, CancellationToken cancellationToken = default)
    {
        var stats = new DashboardStatsDto();
        var nowJakarta = DateTimeHelper.GetJakartaNow();

        // Tentukan date range berdasarkan period di timezone Jakarta
        DateTime? startDateJakarta;
        DateTime endDateJakarta = nowJakarta.AddDays(1).Date;

        startDateJakarta = period?.ToLowerInvariant() switch
        {
            "day"   => nowJakarta.Date,
            "week"  => nowJakarta.Date.AddDays(-6),
            "month" => new DateTime(nowJakarta.Year, nowJakarta.Month, 1),
            "year"  => new DateTime(nowJakarta.Year, 1, 1),
            _       => null  // null = semua data (all time)
        };

        // Konversi batas tanggal ke UTC untuk filter database
        DateTime? startDateUtc = startDateJakarta?.ToUtcFromJakarta();
        DateTime? endDateUtc = endDateJakarta.ToUtcFromJakarta();

        // Load traceability logs sesuai range
        var result = await _traceabilityLogRepository.GetAllTraceabilityNewAsync(
            page: 1, pageSize: int.MaxValue,
            startDate: startDateUtc, endDate: endDateUtc,
            cancellationToken: cancellationToken);
        var allTracLogs = result.Items.ToList();

        // 1. Quality Distribution (Pie Chart) — only finished logs (IsFinish == true)
        int ok = allTracLogs.Count(l => l.IsFinish && l.Status);
        int ng = allTracLogs.Count(l => l.IsFinish && !l.Status);

        stats.QualityDistribution.Add(new ChartDataDto { Label = "OK", Value = ok });
        stats.QualityDistribution.Add(new ChartDataDto { Label = "NG", Value = ng });

        // 2. Top Parts Production (Bar Chart)
        // Ambil SerialNumberClinching dari traceability logs yang sudah terfilter
        var clinchingCodes = allTracLogs
            .Where(l => l.IsFinish && !string.IsNullOrWhiteSpace(l.SerialNumberClinching))
            .Select(l => l.SerialNumberClinching!)
            .Distinct()
            .ToList();

        // Query part dari SerialNumbers berdasarkan clinching codes
        var partCount = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        if (clinchingCodes.Count > 0)
        {
            var serialNumbersWithParts = await _serialNumberRepository.GetAllWithIssuesAndChildRelationsAsync(cancellationToken);
            var filtered = serialNumbersWithParts
                .Where(sn => clinchingCodes.Contains(sn.SerialNumberCode, StringComparer.OrdinalIgnoreCase));

            foreach (var sn in filtered)
            {
                var firstIssue = sn.Issues?.FirstOrDefault(si => si.Issue?.StockIn?.Part != null);
                if (firstIssue?.Issue?.StockIn?.Part != null)
                {
                    string partNumber = firstIssue.Issue.StockIn.Part.Number;
                    partCount[partNumber] = partCount.GetValueOrDefault(partNumber) + 1;
                }
            }
        }

        stats.TopPartsProduction = partCount
            .Select(kvp => new ChartDataDto { Label = kvp.Key, Value = kvp.Value })
            .OrderByDescending(x => x.Value)
            .Take(topPart)
            .ToList();


        // 3. Production Trend — granularitas menyesuaikan period
        switch (period?.ToLowerInvariant())
        {
            case "day":
                // Per jam (24 jam hari ini di waktu Jakarta)
                for (int h = 0; h < 24; h++)
                {
                    var hourStart = nowJakarta.Date.AddHours(h);
                    var hourEnd = hourStart.AddHours(1);
                    int count = allTracLogs.Count(l =>
                    {
                        if (!l.IsFinish) return false;
                        var t = l.CreatedAt.ToJakartaTime();
                        return t >= hourStart && t < hourEnd;
                    });
                    stats.ProductionTrend.Add(new ChartDataDto { Label = hourStart.ToString("HH:00"), Value = count });
                }
                break;

            case "week":
                // Per hari (7 hari terakhir di waktu Jakarta)
                for (int i = 6; i >= 0; i--)
                {
                    var date = nowJakarta.Date.AddDays(-i);
                    var nextDate = date.AddDays(1);
                    int count = allTracLogs.Count(l =>
                    {
                        if (!l.IsFinish) return false;
                        var t = l.CreatedAt.ToJakartaTime();
                        return t >= date && t < nextDate;
                    });
                    stats.ProductionTrend.Add(new ChartDataDto { Label = date.ToString("dd MMM"), Value = count });
                }
                break;

            case "month":
                // Per hari dalam bulan ini di waktu Jakarta
                var daysInMonth = DateTime.DaysInMonth(nowJakarta.Year, nowJakarta.Month);
                for (int d = 1; d <= daysInMonth; d++)
                {
                    var date = new DateTime(nowJakarta.Year, nowJakarta.Month, d);
                    var nextDate = date.AddDays(1);
                    int count = allTracLogs.Count(l =>
                    {
                        if (!l.IsFinish) return false;
                        var t = l.CreatedAt.ToJakartaTime();
                        return t >= date && t < nextDate;
                    });
                    stats.ProductionTrend.Add(new ChartDataDto { Label = date.ToString("dd MMM"), Value = count });
                }
                break;

            default:
                // year atau all-time: per bulan
                // Untuk all-time, ambil bulan dari log paling lama hingga sekarang
                DateTime trendStart = period?.ToLowerInvariant() == "year"
                    ? new DateTime(nowJakarta.Year, 1, 1)
                    : (allTracLogs.Count > 0
                        ? new DateTime(allTracLogs.Min(l => l.CreatedAt.ToJakartaTime()).Year,
                                       allTracLogs.Min(l => l.CreatedAt.ToJakartaTime()).Month, 1)
                        : new DateTime(nowJakarta.Year, 1, 1));

                var cursor = trendStart;
                while (cursor <= nowJakarta)
                {
                    var nextMonth = cursor.AddMonths(1);
                    int count = allTracLogs.Count(l =>
                    {
                        if (!l.IsFinish) return false;
                        var t = l.CreatedAt.ToJakartaTime();
                        return t >= cursor && t < nextMonth;
                    });
                    stats.ProductionTrend.Add(new ChartDataDto { Label = cursor.ToString("MMM yyyy"), Value = count });
                    cursor = nextMonth;
                }
                break;
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

    public async Task<TraceabilityFlowDto> GetTraceabilityFlowAsync(CancellationToken cancellationToken = default)
    {
        var flow = new TraceabilityFlowDto
        {
            Stats = new FlowStatsDto { LastUpdated = DateTime.Now }
        };

        // Get all serial numbers with child relations
        var allSerialNumbersEnumerable = await _serialNumberRepository.GetAllWithChildRelationsAsync(cancellationToken);
        var serialNumberList = allSerialNumbersEnumerable.ToList();

        // Get process logs with eager loading for Details and Process
        var allLogsEnumerable = await _processLogRepository.GetAllWithDetailsAsync(cancellationToken);
        var allLogs = allLogsEnumerable.ToList();

        // Get process logs grouped by serial number ID
        var logsBySerialNumber = allLogs
            .GroupBy(l => l.SerialNumberId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Create serial lookup by ID
        var serialById = serialNumberList.ToDictionary(s => s.Id);

        // Create child lookup (parentId -> child serial)
        var childByParentId = new Dictionary<int, SerialNumber>();
        foreach (var serial in serialNumberList)
        {
            var childRelations = serial.ChildRelations?.ToList() ?? new List<SerialNumberRelation>();
            foreach (var rel in childRelations)
            {
                // Get child serial by ID from serialById lookup
                if (serialById.ContainsKey(rel.ChildSerialNumberId))
                {
                    childByParentId[serial.Id] = serialById[rel.ChildSerialNumberId];
                }
            }
        }

        // Initialize stations
        for (int i = 0; i < ProcessOrder.Length; i++)
        {
            flow.Stations.Add(new StationDto
            {
                Order = i + 1,
                Code = ProcessOrder[i],
                Name = GetProcessDisplayName(ProcessOrder[i]),
                Items = new List<SerialNumberItemDto>()
            });
        }

        // Track processed serial numbers
        var processedSerialIds = new HashSet<int>();

        // Helper function to get station index from process code
        int GetStationIndex(string processCode)
        {
            return Array.IndexOf(ProcessOrder, processCode);
        }

        // Helper function to add serial to flow
        void AddToFlow(SerialNumber serial, ProcessLog? latestLog)
        {
            var item = new SerialNumberItemDto
            {
                Id = serial.Id,
                SerialNumberCode = serial.SerialNumberCode,
                CurrentProcess = latestLog?.Details?.FirstOrDefault()?.Process?.Code,
                CreatedAt = serial.CreatedAt
            };

            if (latestLog == null || !latestLog.Details.Any())
            {
                flow.Queue.Add(item);
                processedSerialIds.Add(serial.Id);
                return;
            }

            var latestDetail = latestLog.Details.OrderByDescending(d => d.CreatedAt).FirstOrDefault();
            var processCode = latestDetail?.Process?.Code ?? string.Empty;
            var stationIndex = GetStationIndex(processCode);

            // Check if finished
            if (latestLog.IsFinished)
            {
                if (latestLog.Status) // OK
                {
                    flow.OkList.Add(item);
                }
                else // NG
                {
                    flow.NgList.Add(item);
                }
                processedSerialIds.Add(serial.Id);
                return;
            }

            // Still in progress - put in station
            if (stationIndex >= 0 && stationIndex < flow.Stations.Count)
            {
                flow.Stations[stationIndex].Items.Add(item);
                processedSerialIds.Add(serial.Id);
            }
            else
            {
                flow.Queue.Add(item);
                processedSerialIds.Add(serial.Id);
            }
        }

        // Process each serial number
        foreach (var serial in serialNumberList)
        {
            if (processedSerialIds.Contains(serial.Id))
                continue;

            if (!logsBySerialNumber.TryGetValue(serial.Id, out var serialLogs) || !serialLogs.Any())
            {
                // No process log yet - add to queue
                flow.Queue.Add(new SerialNumberItemDto
                {
                    Id = serial.Id,
                    SerialNumberCode = serial.SerialNumberCode,
                    CreatedAt = serial.CreatedAt
                });
                processedSerialIds.Add(serial.Id);
                continue;
            }

            var latestLog = serialLogs.OrderByDescending(l => l.CreatedAt).FirstOrDefault();
            var latestDetail = latestLog?.Details?.OrderByDescending(d => d.CreatedAt).FirstOrDefault();
            var processCode = latestDetail?.Process?.Code ?? string.Empty;
            var stationIndex = GetStationIndex(processCode);

            // Check if this is PVRA/CC (parent) and finished at HE LEAK
            if ((serial.SerialNumberCode.StartsWith("PVRA") || serial.SerialNumberCode.StartsWith("CC")) && 
                latestLog?.IsFinished == true && 
                stationIndex == 2) // HE LEAK is at index 2
            {
                // Find child MF serial number
                if (childByParentId.TryGetValue(serial.Id, out var childMF))
                {
                    // Check if child MF has process logs
                    if (logsBySerialNumber.TryGetValue(childMF.Id, out var childLogs) && childLogs.Any())
                    {
                        // Follow child's process
                        AddToFlow(childMF, childLogs.OrderByDescending(l => l.CreatedAt).FirstOrDefault());
                        processedSerialIds.Add(serial.Id); // Mark parent as processed
                        continue;
                    }
                    else
                    {
                        // Child MF has no process log yet - put child MF in M_FAN_ASSY station
                        var mfanAssyIndex = GetStationIndex("M_FAN_ASSY");
                        if (mfanAssyIndex >= 0)
                        {
                            var item = new SerialNumberItemDto
                            {
                                Id = childMF.Id,
                                SerialNumberCode = childMF.SerialNumberCode,
                                CurrentProcess = "M_FAN_ASSY",
                                CreatedAt = childMF.CreatedAt
                            };
                            flow.Stations[mfanAssyIndex].Items.Add(item);
                            processedSerialIds.Add(serial.Id);
                            processedSerialIds.Add(childMF.Id);
                            continue;
                        }
                    }
                }
                
                // If no child relation, mark as OK
                AddToFlow(serial, latestLog);
            }
            else
            {
                // Normal flow
                AddToFlow(serial, latestLog);
            }
        }

        // Update stats
        flow.Stats.QueueCount = flow.Queue.Count;
        flow.Stats.InProgressCount = flow.Stations.Sum(s => s.Items.Count);
        flow.Stats.OkCount = flow.OkList.Count;
        flow.Stats.NgCount = flow.NgList.Count;

        // Sort items by created date (newest first)
        flow.Queue = flow.Queue.OrderByDescending(q => q.CreatedAt).ToList();
        flow.OkList = flow.OkList.OrderByDescending(o => o.CreatedAt).ToList();
        flow.NgList = flow.NgList.OrderByDescending(n => n.CreatedAt).ToList();

        foreach (var station in flow.Stations)
        {
            station.Items = station.Items.OrderByDescending(i => i.CreatedAt).ToList();
        }

        return flow;
    }

    private static string GetProcessDisplayName(string code)
    {
        return code switch
        {
            "CLINCHING_SHORT_SIDE" => "Clinch Short",
            "CLINCHING_LONG_SIDE" => "Clinch Long",
            "HE_LEAK" => "HE Leak",
            "M_FAN_ASSY" => "M-Fan Assy",
            "M_FAN_INSPECTION" => "M-Fan Insp",
            "ECM_ASSY" => "ECM Assy",
            "FINAL_INSPECTION" => "Final Insp",
            _ => code
        };
    }

    private double CalculateYield(int total, int ok)
    {
        if (total == 0) return 0;
        return Math.Round((double)ok / total * 100, 2);
    }
}
