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
    private readonly IProcessRepository _processRepository;

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
        IProcessRepository processRepository)
    {
        _processLogRepository = processLogRepository;
        _partRepository = partRepository;
        _issueRepository = issueRepository;
        _processLogService = processLogService;
        _traceabilitySummarySimulator = traceabilitySummarySimulator;
        _serialNumberRepository = serialNumberRepository;
        _processRepository = processRepository;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.Now;
        var todayStart = now.Date;
        var monthStart = new DateTime(now.Year, now.Month, 1);

        var summary = new DashboardSummaryDto();

        summary.Today.TotalProduction = await _processLogRepository.GetTotalProductionAsync(todayStart, cancellationToken);
        summary.Today.OkCount = await _processLogRepository.GetOkCountAsync(todayStart, cancellationToken);
        summary.Today.NgCount = await _processLogRepository.GetNgCountAsync(todayStart, cancellationToken);
        summary.Today.YieldRate = CalculateYield(summary.Today.TotalProduction, summary.Today.OkCount);

        summary.ThisMonth.TotalProduction = await _processLogRepository.GetTotalProductionAsync(monthStart, cancellationToken);
        summary.ThisMonth.OkCount = await _processLogRepository.GetOkCountAsync(monthStart, cancellationToken);
        summary.ThisMonth.NgCount = await _processLogRepository.GetNgCountAsync(monthStart, cancellationToken);
        summary.ThisMonth.YieldRate = CalculateYield(summary.ThisMonth.TotalProduction, summary.ThisMonth.OkCount);

        summary.Total.TotalProduction = await _processLogRepository.GetTotalProductionAsync(null, cancellationToken);
        summary.Total.OkCount = await _processLogRepository.GetOkCountAsync(null, cancellationToken);
        summary.Total.NgCount = await _processLogRepository.GetNgCountAsync(null, cancellationToken);
        summary.Total.YieldRate = CalculateYield(summary.Total.TotalProduction, summary.Total.OkCount);


        return summary;
    }

    public Task<List<DashboardSummaryFieldDto>> GetTraceabilitySummaryAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_traceabilitySummarySimulator.GetSnapshot());
    }

    public async Task<DashboardStatsDto> GetStatsAsync(
    int topParts = 5,
    int trendDays = 7,
    CancellationToken cancellationToken = default)
    {
        var stats = new DashboardStatsDto();
        var now = DateTime.Now;
        var startDate = DateTime.Today.AddDays(-(trendDays - 1));
        var monthStart = new DateTime(now.Year, now.Month, 1);
        // Quality Distribution

        // Quality Distribution
        var ok = await _processLogRepository.GetOkCountAsync(startDate, cancellationToken);
        var ng = await _processLogRepository.GetNgCountAsync(startDate, cancellationToken);

        stats.QualityDistribution.Add(new ChartDataDto
        {
            Label = "OK",
            Value = ok
        });

        stats.QualityDistribution.Add(new ChartDataDto
        {
            Label = "NG",
            Value = ng
        });

        // Top Parts Production
        var topPartsData = await _processLogRepository.GetTopPartsProductionAsync(
            monthStart,
            topParts,
            cancellationToken);

        stats.TopPartsProduction = topPartsData
            .Select(x => new ChartDataDto
            {
                Label = x.Label,
                Value = x.Value
            })
            .ToList();

        // Production Trend
        var productionTrend = await _processLogRepository.GetProductionTrendAsync(
            trendDays,
            cancellationToken);

        stats.ProductionTrend = productionTrend
            .Select(x => new ChartDataDto
            {
                Label = x.Label,
                Value = x.Value
            })
            .ToList();

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

            // Check if this is CC (parent) and finished at HE LEAK
            if (serial.SerialNumberCode.StartsWith("CC") && 
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
