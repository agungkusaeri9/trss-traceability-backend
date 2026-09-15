using Mapster;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TraceabilitySystem.Application.DTOs.Pagination;
using TraceabilitySystem.Application.DTOs.ProcessLog;
using TraceabilitySystem.Application.DTOs.TraceabilityLog;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Application.Mappers;
using TraceabilitySystem.Domain.Entities;
using TraceabilitySystem.Domain.Interfaces;
using TraceabilitySystem.Shared.Exceptions;
using TraceabilitySystem.Shared.Helpers;
using TraceabilitySystem.Shared.Models;

namespace TraceabilitySystem.Application.Services;

public class TraceabilityLogService : ITraceabilityLogService
{
    private static readonly HashSet<string> OverallProcessCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "ECM_ASSY",
        "FINAL_INSPECTION"
    };

    private readonly ITraceabilityLogRepository _traceabilityLogRepository;
    private readonly IProcessRepository _processRepository;
    private readonly IParameterRepository _parameterRepository;
    private readonly IUserRepository _userRepository;

    public TraceabilityLogService(
        ITraceabilityLogRepository traceabilityLogRepository,
        IProcessRepository processRepository,
        IParameterRepository parameterRepository,
        IUserRepository userRepository)
    {
        _traceabilityLogRepository = traceabilityLogRepository;
        _processRepository = processRepository;
        _parameterRepository = parameterRepository;
        _userRepository = userRepository;
    }

    public async Task<PagedResult<ProcessLogMockDto>> GetTraceabilityLogsAsync(
        int page,
        int pageSize,
        string? serialNumberCode = null,
        bool? status = null,
        bool? isFinished = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _traceabilityLogRepository.GetPagedLogsAsync(
            page,
            pageSize,
            serialNumberCode,
            status,
            isFinished,
            startDate,
            endDate,
            clinchingOnly: true,
            cancellationToken);

        var mapped = items.Select(MapToMockDto).ToList();

        return new PagedResult<ProcessLogMockDto>
        {
            Items = mapped,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ProcessLogMockDto> GetTraceabilityLogByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var log = await _traceabilityLogRepository.GetLogWithDetailsAsync(id, cancellationToken);
        if (log == null) throw new NotFoundException(nameof(ProcessLog), id);

        return MapToMockDto(log);
    }

    public async Task<ProcessLogMockDto> GetTraceabilityLogBySerialNumberAsync(string serialNumber, CancellationToken cancellationToken = default)
    {
        var log = await _traceabilityLogRepository.GetLogBySerialNumberAsync(serialNumber, cancellationToken);
        if (log == null) throw new NotFoundException(nameof(ProcessLog), serialNumber);

        return MapToMockDto(log);
    }

    public async Task<ProcessLogFullValueDto> GetTraceabilityLogFullValuesAsync(string serialNumberCode, CancellationToken cancellationToken = default)
    {
        var log = await _traceabilityLogRepository.GetProcessLogFullValueAsync(serialNumberCode, cancellationToken);
        if (log == null) throw new NotFoundException(nameof(ProcessLog), serialNumberCode);

        var childRel = log.SerialNumber.ParentRelations?.FirstOrDefault();
        var childSn = childRel?.ChildSerialNumber;
        var childLog = childSn?.ProcessLogs?.OrderByDescending(x => x.CreatedAt).FirstOrDefault();

        var ecmDetailsFull = log.Details?
            .Where(d => string.Equals(d.Process?.Code, "ECM_ASSY", StringComparison.OrdinalIgnoreCase))
            .ToList() ?? new List<ProcessLogDetail>();
        bool ecmStatusFull = ecmDetailsFull.Count == 0 || ecmDetailsFull.All(d => d.Status && (d.ValueBoolean == null || d.ValueBoolean.Value));

        var finalDetailsFull = log.Details?
            .Where(d => string.Equals(d.Process?.Code, "FINAL_INSPECTION", StringComparison.OrdinalIgnoreCase))
            .ToList() ?? new List<ProcessLogDetail>();
        bool finalInspectionStatusFull = finalDetailsFull.Count == 0 || finalDetailsFull.All(d => d.Status && (d.ValueBoolean == null || d.ValueBoolean.Value));

        bool calculatedFullStatus = log.Status && (childLog == null || childLog.Status) && ecmStatusFull && finalInspectionStatusFull;

        var result = new ProcessLogFullValueDto
        {
            Id = log.Id,
            SerialNumberCode = log.SerialNumber.SerialNumberCode,
            Status = calculatedFullStatus,
            IsFinished = log.IsFinished,
            CreatedAt = log.CreatedAt,
            UpdatedAt = log.UpdatedAt
        };

        var parentIssues = log.SerialNumber?.Issues?
            .Where(x => x.Issue != null)
            .OrderBy(x => x.CreatedAt)
            .ToList() ?? new List<SerialNumberIssue>();

        string coreAsm = parentIssues.FirstOrDefault(i => i.Issue?.StockIn?.Part?.Name?.Contains("Core", StringComparison.OrdinalIgnoreCase) == true)?.Issue?.Number
            ?? GetIssueNumberFromDetail(FindDetail(log.Details, "CLINCHING_SHORT_SIDE", "CORE_ASM_VALUE", "CORE_ASM_RESULT", "CORE_ASM", "CORE_ASM_ISSUE_NO"))
            ?? (parentIssues.Count > 0 ? parentIssues[0].Issue?.Number : null)
            ?? string.Empty;

        string upperTank = parentIssues.FirstOrDefault(i => i.Issue?.StockIn?.Part?.Name?.Contains("Upper", StringComparison.OrdinalIgnoreCase) == true)?.Issue?.Number
            ?? GetIssueNumberFromDetail(FindDetail(log.Details, "CLINCHING_SHORT_SIDE", "UPPER_TANK_ASM_VALUE", "UPPER_TANK_ASM_RESULT", "UPPER_TANK_ASM", "UPPER_TANK_ASM_ISSUE_NO"))
            ?? (parentIssues.Count > 1 ? parentIssues[1].Issue?.Number : null)
            ?? string.Empty;

        string lowerTank = parentIssues.FirstOrDefault(i => i.Issue?.StockIn?.Part?.Name?.Contains("Lower", StringComparison.OrdinalIgnoreCase) == true)?.Issue?.Number
            ?? GetIssueNumberFromDetail(FindDetail(log.Details, "CLINCHING_SHORT_SIDE", "LOWER_TANK_ASM_VALUE", "LOWER_TANK_ASM_RESULT", "LOWER_TANK_ASM", "LOWER_TANK_ASM_ISSUE_NO"))
            ?? (parentIssues.Count > 2 ? parentIssues[2].Issue?.Number : null)
            ?? string.Empty;

        result.Clinching = new ProcessLogFullValueParentDto
        {
            SerialNumberCode = log.SerialNumber.SerialNumberCode,
            Details = log.Details
                .Where(x => !OverallProcessCodes.Contains(x.Process.Code) && (x.Parameter == null || x.Parameter.ShowInDisplay))
                .OrderBy(x => string.Equals(x.Process.Code, "HE_LEAK", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenBy(x => x.Process.Order)
                .ThenBy(x => x.Parameter.Order)
                .Adapt<List<ProcessLogFullValueDetailDto>>()
        };

        SetOrAddDetail(result.Clinching.Details, "CLINCHING_SHORT_SIDE", "Clinching Short Side", "CORE_ASM_VALUE", "Core Asm", coreAsm);
        SetOrAddDetail(result.Clinching.Details, "CLINCHING_SHORT_SIDE", "Clinching Short Side", "UPPER_TANK_ASM_VALUE", "Upper Tank Asm", upperTank);
        SetOrAddDetail(result.Clinching.Details, "CLINCHING_SHORT_SIDE", "Clinching Short Side", "LOWER_TANK_ASM_VALUE", "Lower Tank Asm", lowerTank);

        childRel = log.SerialNumber.ParentRelations?.FirstOrDefault();
        childSn = childRel?.ChildSerialNumber;
        childLog = childSn?.ProcessLogs?.OrderByDescending(x => x.CreatedAt).FirstOrDefault();
        var childIssues = childSn?.Issues?
            .Where(x => x.Issue != null)
            .OrderBy(x => x.CreatedAt)
            .ToList() ?? new List<SerialNumberIssue>();
        var childDetails = childLog?.Details ?? childSn?.ProcessLogs?.SelectMany(pl => pl.Details).ToList() ?? new List<ProcessLogDetail>();

        string? lotFan = childIssues.FirstOrDefault(i => i.Issue?.StockIn?.Part?.Name?.Contains("Fan", StringComparison.OrdinalIgnoreCase) == true &&
                                                        i.Issue?.StockIn?.Part?.Name?.Contains("Motor", StringComparison.OrdinalIgnoreCase) != true &&
                                                        i.Issue?.StockIn?.Part?.Name?.Contains("Guide", StringComparison.OrdinalIgnoreCase) != true)?.Issue?.Number
            ?? GetIssueNumberFromDetail(FindDetail(childDetails, "M_FAN_ASSY", "LOT_FAN_ASM_RESULT", "LOT_FAN_ASM", "FAN_ASM", "FAN_ASM_RESULT", "FAN_ASM_ISSUE_NO"))
            ?? (childIssues.Count > 0 ? childIssues[0].Issue?.Number : null);

        string? lotMotor = childIssues.FirstOrDefault(i => i.Issue?.StockIn?.Part?.Name?.Contains("Motor", StringComparison.OrdinalIgnoreCase) == true)?.Issue?.Number
            ?? GetIssueNumberFromDetail(FindDetail(childDetails, "M_FAN_ASSY", "LOT_MOTOR_ASM_RESULT", "LOT_MOTOR_ASM", "MOTOR_ASM", "MOTOR_ASM_RESULT", "FAN_MOTOR_ASM_ISSUE_NO", "MOTOR_ASM_ISSUE_NO"))
            ?? (childIssues.Count > 1 ? childIssues[1].Issue?.Number : null);

        string? lotGuide = childIssues.FirstOrDefault(i => i.Issue?.StockIn?.Part?.Name?.Contains("Guide", StringComparison.OrdinalIgnoreCase) == true)?.Issue?.Number
            ?? GetIssueNumberFromDetail(FindDetail(childDetails, "M_FAN_ASSY", "LOT_GUIDE_ASM_RESULT", "LOT_GUIDE_ASM", "GUIDE_ASM", "FUN_GUIDE_ASM_RESULT", "FAN_GUIDE_ASM_RESULT", "FAN_GUIDE_ASM_ISSUE_NO", "GUIDE_ASM_ISSUE_NO"))
            ?? (childIssues.Count > 2 ? childIssues[2].Issue?.Number : null);

        if (childSn != null)
        {
            result.MFan = new ProcessLogFullValueChildDto
            {
                SerialNumberCode = childSn.SerialNumberCode,
                Details = childDetails
                    .Where(x => x.Parameter == null || x.Parameter.ShowInDisplay)
                    .OrderBy(x => x.Process.Order)
                    .ThenBy(x => x.Parameter.Order)
                    .Adapt<List<ProcessLogFullValueDetailDto>>()
            };

            SetOrAddDetail(result.MFan.Details, "M_FAN_ASSY", "M-Fan Assembly", "LOT_FAN_ASM_RESULT", "Lot Fan Asm", lotFan);
            SetOrAddDetail(result.MFan.Details, "M_FAN_ASSY", "M-Fan Assembly", "LOT_MOTOR_ASM_RESULT", "Lot Motor Asm", lotMotor);
            SetOrAddDetail(result.MFan.Details, "M_FAN_ASSY", "M-Fan Assembly", "LOT_GUIDE_ASM_RESULT", "Lot Guide Asm", lotGuide);
        }

        result.Overall = log.Details
            .Where(x => OverallProcessCodes.Contains(x.Process.Code) && (x.Parameter == null || x.Parameter.ShowInDisplay))
            .OrderBy(x => x.Process.Order)
            .ThenBy(x => x.Parameter.Order)
            .Adapt<List<ProcessLogFullValueDetailDto>>();

        return result;
    }

    private static void SetOrAddDetail(
        List<ProcessLogFullValueDetailDto> details,
        string processCode,
        string processName,
        string paramCode,
        string paramName,
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;

        var existing = details.FirstOrDefault(d =>
            (string.Equals(d.ProcessCode, processCode, StringComparison.OrdinalIgnoreCase) ||
             d.ProcessName.Contains(processName, StringComparison.OrdinalIgnoreCase)) &&
            (d.ParameterCode.Contains(paramCode, StringComparison.OrdinalIgnoreCase) ||
             d.ParameterName.Contains(paramName, StringComparison.OrdinalIgnoreCase) ||
             paramCode.Contains(d.ParameterCode, StringComparison.OrdinalIgnoreCase) ||
             paramName.Contains(d.ParameterName, StringComparison.OrdinalIgnoreCase)));

        if (existing != null)
        {
            existing.Value = value;
        }
        else
        {
            details.Add(new ProcessLogFullValueDetailDto
            {
                ProcessCode = processCode,
                ProcessName = processName,
                ParameterCode = paramCode,
                ParameterName = paramName,
                Value = value
            });
        }
    }

    // ── Mapping Helpers ─────────────────────────────────────────────────────────

    public static ProcessLogMockDto MapToMockDto(ProcessLog log)
    {
        var sn = log.SerialNumber;
        var clinchingSn = sn?.SerialNumberCode ?? string.Empty;

        // Child SN (M-Fan)
        var childRelation = sn?.ParentRelations?.FirstOrDefault();
        var childSn = childRelation?.ChildSerialNumber;
        var childLog = childSn?.ProcessLogs?.OrderByDescending(x => x.CreatedAt).FirstOrDefault();
        var mFanSn = childSn?.SerialNumberCode;

        var parentDetails = log.Details ?? new List<ProcessLogDetail>();
        var childDetails = childLog?.Details ?? childSn?.ProcessLogs?.SelectMany(pl => pl.Details).ToList() ?? new List<ProcessLogDetail>();

        // 1. Clinching Short Side Issues / Lots
        var parentIssues = sn?.Issues?
            .Where(x => x.Issue != null)
            .OrderBy(x => x.CreatedAt)
            .ToList() ?? new List<SerialNumberIssue>();

        string coreAsm = parentIssues.FirstOrDefault(i => i.Issue?.StockIn?.Part?.Name?.Contains("Core", StringComparison.OrdinalIgnoreCase) == true)?.Issue?.Number
            ?? GetIssueNumberFromDetail(FindDetail(parentDetails, "CLINCHING_SHORT_SIDE", "CORE_ASM_VALUE", "CORE_ASM_RESULT", "CORE_ASM", "CORE_ASM_ISSUE_NO"))
            ?? (parentIssues.Count > 0 ? parentIssues[0].Issue?.Number : null)
            ?? string.Empty;

        string upperTank = parentIssues.FirstOrDefault(i => i.Issue?.StockIn?.Part?.Name?.Contains("Upper", StringComparison.OrdinalIgnoreCase) == true)?.Issue?.Number
            ?? GetIssueNumberFromDetail(FindDetail(parentDetails, "CLINCHING_SHORT_SIDE", "UPPER_TANK_ASM_VALUE", "UPPER_TANK_ASM_RESULT", "UPPER_TANK_ASM", "UPPER_TANK_ASM_ISSUE_NO"))
            ?? (parentIssues.Count > 1 ? parentIssues[1].Issue?.Number : null)
            ?? string.Empty;

        string lowerTank = parentIssues.FirstOrDefault(i => i.Issue?.StockIn?.Part?.Name?.Contains("Lower", StringComparison.OrdinalIgnoreCase) == true)?.Issue?.Number
            ?? GetIssueNumberFromDetail(FindDetail(parentDetails, "CLINCHING_SHORT_SIDE", "LOWER_TANK_ASM_VALUE", "LOWER_TANK_ASM_RESULT", "LOWER_TANK_ASM", "LOWER_TANK_ASM_ISSUE_NO"))
            ?? (parentIssues.Count > 2 ? parentIssues[2].Issue?.Number : null)
            ?? string.Empty;

        int? oRingSet = GetDetailInt(FindDetail(parentDetails, "CLINCHING_SHORT_SIDE", "O_RING_SET_RESULT", "O_RING_SET", "ORING_SET_RESULT"));
        string ngBoxShort = GetDetailText(FindDetail(parentDetails, "CLINCHING_SHORT_SIDE", "NG_BOX_SENSOR_SHORT_SIDE_VALUE", "NG_BOX_SENSOR_SHORT_SIDE", "NG_BOX_SHORT_SIDE", "NG_BOX")) ?? "ON";

        // 2. Clinching Long Side
        var clinchingHeightDetails = parentDetails
            .Where(d => string.Equals(d.Process?.Code, "CLINCHING_LONG_SIDE", StringComparison.OrdinalIgnoreCase) &&
                        (d.Parameter?.Code?.StartsWith("CLINCHING_HEIGHT", StringComparison.OrdinalIgnoreCase) == true ||
                         d.Parameter?.Name?.Contains("Height", StringComparison.OrdinalIgnoreCase) == true))
            .OrderBy(d => d.Parameter?.Order ?? d.Id)
            .ToList();

        double[] clinchingHeightValues;
        if (clinchingHeightDetails.Count > 0)
        {
            clinchingHeightValues = clinchingHeightDetails
                .Select(d => GetDetailNumber(d) ?? 0.0)
                .ToArray();
        }
        else
        {
            clinchingHeightValues = Array.Empty<double>();
        }

        bool? clinchingHeightStatus = clinchingHeightValues.Length > 0 ? clinchingHeightValues.All(x => x == 1) : null;

        var endPlateDetails = parentDetails
            .Where(d => string.Equals(d.Process?.Code, "CLINCHING_LONG_SIDE", StringComparison.OrdinalIgnoreCase) &&
                        (d.Parameter?.Code?.StartsWith("END_PLATE_WIDTH", StringComparison.OrdinalIgnoreCase) == true ||
                         d.Parameter?.Name?.Contains("End Plate", StringComparison.OrdinalIgnoreCase) == true))
            .OrderBy(d => d.Parameter?.Order ?? d.Id)
            .ToList();

        int[] endPlateResults = endPlateDetails.Select(d => GetDetailInt(d) ?? 0).ToArray();
        bool? endPlateStatus = endPlateResults.Length > 0 ? endPlateResults.All(x => x == 1) : null;
        string ngBoxLong = GetDetailText(FindDetail(parentDetails, "CLINCHING_LONG_SIDE", "NG_BOX_SENSOR_LONG_SIDE_VALUE", "NG_BOX_SENSOR_LONG_SIDE", "NG_BOX_LONG_SIDE", "NG_BOX")) ?? "ON";

        // 2.5. HE Leak
        int? capTypePos = GetDetailInt(FindDetail(parentDetails, "HE_LEAK", "CAP_TYPE_POSITION_RESULT", "CAP_TYPE_POSITION", "CAP_TYPE"));
        int? leakResult = GetDetailInt(FindDetail(parentDetails, "HE_LEAK", "LEAK_RESULT", "LEAK_TEST_RESULT", "LEAK_STATUS"));
        double? leakValue = GetDetailNumber(FindDetail(parentDetails, "HE_LEAK", "LEAK_LAST_LEAKAGE_VALUE", "LEAK_LAST_LEAKAGE", "LEAK_VALUE", "LEAKAGE_VALUE"));

        // 3. M-Fan Assy Lots & Details
        var childIssues = childSn?.Issues?
            .Where(x => x.Issue != null)
            .OrderBy(x => x.CreatedAt)
            .ToList() ?? new List<SerialNumberIssue>();

        string? lotFan = childIssues.FirstOrDefault(i => i.Issue?.StockIn?.Part?.Name?.Contains("Fan", StringComparison.OrdinalIgnoreCase) == true &&
                                                        i.Issue?.StockIn?.Part?.Name?.Contains("Motor", StringComparison.OrdinalIgnoreCase) != true &&
                                                        i.Issue?.StockIn?.Part?.Name?.Contains("Guide", StringComparison.OrdinalIgnoreCase) != true)?.Issue?.Number
            ?? GetIssueNumberFromDetail(FindDetail(childDetails, "M_FAN_ASSY", "LOT_FAN_ASM_RESULT", "LOT_FAN_ASM", "FAN_ASM", "FAN_ASM_RESULT", "FAN_ASM_ISSUE_NO"))
            ?? (childIssues.Count > 0 ? childIssues[0].Issue?.Number : null);

        string? lotMotor = childIssues.FirstOrDefault(i => i.Issue?.StockIn?.Part?.Name?.Contains("Motor", StringComparison.OrdinalIgnoreCase) == true)?.Issue?.Number
            ?? GetIssueNumberFromDetail(FindDetail(childDetails, "M_FAN_ASSY", "LOT_MOTOR_ASM_RESULT", "LOT_MOTOR_ASM", "MOTOR_ASM", "MOTOR_ASM_RESULT", "FAN_MOTOR_ASM_ISSUE_NO", "MOTOR_ASM_ISSUE_NO"))
            ?? (childIssues.Count > 1 ? childIssues[1].Issue?.Number : null);

        string? lotGuide = childIssues.FirstOrDefault(i => i.Issue?.StockIn?.Part?.Name?.Contains("Guide", StringComparison.OrdinalIgnoreCase) == true)?.Issue?.Number
            ?? GetIssueNumberFromDetail(FindDetail(childDetails, "M_FAN_ASSY", "LOT_GUIDE_ASM_RESULT", "LOT_GUIDE_ASM", "GUIDE_ASM", "FUN_GUIDE_ASM_RESULT", "FAN_GUIDE_ASM_RESULT", "FAN_GUIDE_ASM_ISSUE_NO", "GUIDE_ASM_ISSUE_NO"))
            ?? (childIssues.Count > 2 ? childIssues[2].Issue?.Number : null);

        string? boltTighten = GetDetailText(FindDetail(childDetails, "M_FAN_ASSY", "BOLT_TIGHTEN_VALUE", "BOLT_TIGHTEN"));
        string? boltQty = GetDetailText(FindDetail(childDetails, "M_FAN_ASSY", "BOLT_TIGHTEN_QTY_VALUE", "BOLT_TIGHTEN_QTY", "BOLT_QTY"));
        string? nutTighten = GetDetailText(FindDetail(childDetails, "M_FAN_ASSY", "NUT_TIGHTEN_VALUE", "NUT_TIGHTEN"));

        // 4. M-Fan Inspection (Comprehensive alias mapping)
        double? rotMax = GetDetailNumber(FindDetail(childDetails, null, "M_FAN_INSPECTION_ROTATION_SPEED_MAX_VALUE", "M_FAN_INSPECTION_ROTATION_SPEED_MAX", "ROTATION_SPEED_MAX", "ROT_MAX"));
        double? rotMin = GetDetailNumber(FindDetail(childDetails, null, "M_FAN_INSPECTION_ROTATION_SPEED_MIN_VALUE", "M_FAN_INSPECTION_ROTATION_SPEED_MIN", "ROTATION_SPEED_MIN", "ROT_MIN"));
        double? ampMax = GetDetailNumber(FindDetail(childDetails, null, "M_FAN_INSPECTION_AMPERE_MAX_VALUE", "M_FAN_INSPECTION_AMPERE_MAX", "AMPERE_MAX", "AMP_MAX"));
        double? ampMin = GetDetailNumber(FindDetail(childDetails, null, "M_FAN_INSPECTION_AMPERE_MIN_VALUE", "M_FAN_INSPECTION_AMPERE_MIN", "AMPERE_MIN", "AMP_MIN"));
        string? windDir = GetDetailText(FindDetail(childDetails, null, "M_FAN_INSPECTION_WIND_DIRECTION_VALUE", "M_FAN_INSPECTION_WIND_DIRECTION", "WIND_DIRECTION"));
        int? mFanTest = GetDetailInt(FindDetail(childDetails, null, "M_FAN_TEST_RESULT", "MFAN_TEST_RESULT", "TEST_RESULT"));
        string? ngBoxMFan = GetDetailText(FindDetail(childDetails, null, "NG_BOX_SENSOR_M_FAN_INSPECTION_VALUE", "NG_BOX_SENSOR_M_FAN_INSPECTION", "NG_BOX_MFAN", "NG_BOX")) ?? "ON";

        // 5. ECM Assy
        int? radCoreLabel = GetDetailInt(FindDetail(parentDetails, "ECM_ASSY", "RAD_CORE_ASM_NAME_LABEL_RESULT", "RAD_CORE_LABEL", "RAD_CORE_ASM_LABEL"));
        int? motorFanLabel = GetDetailInt(FindDetail(parentDetails, "ECM_ASSY", "MOTOR_FAN_ASSY_LABEL_RESULT", "MOTOR_FAN_LABEL", "MOTOR_FAN_ASSY_LABEL"));
        double? ecmBolt = GetDetailNumber(FindDetail(parentDetails, "ECM_ASSY", "ECM_ASSY_BOLT_TIGHTEN_VALUE", "ECM_BOLT_TIGHTEN", "ECM_BOLT"));
        double? ecmBoltQty = GetDetailNumber(FindDetail(parentDetails, "ECM_ASSY", "ECM_ASSY_BOLT_TIGHTEN_QTY_VALUE", "ECM_BOLT_QTY", "ECM_ASSY_BOLT_QTY"));
        string? ngBoxEcm = GetDetailText(FindDetail(parentDetails, "ECM_ASSY", "NG_BOX_SENSOR_ECM_ASSY_VALUE", "NG_BOX_SENSOR_ECM_ASSY", "NG_BOX_ECM", "NG_BOX")) ?? "ON";

        // 6. Final Inspection
        int? finalRadCoreLabel = GetDetailInt(FindDetail(parentDetails, "FINAL_INSPECTION", "FINAL_INSPECTION_RAD_CORE_ASM_NAME_LABEL_RESULT", "FINAL_RAD_CORE_LABEL"));

        var checkPointDetails = parentDetails
            .Where(d => string.Equals(d.Process?.Code, "FINAL_INSPECTION", StringComparison.OrdinalIgnoreCase) &&
                        (d.Parameter?.Code?.StartsWith("CHECK_POINT", StringComparison.OrdinalIgnoreCase) == true ||
                         d.Parameter?.Name?.Contains("Check Point", StringComparison.OrdinalIgnoreCase) == true))
            .OrderBy(d => d.Parameter?.Order ?? d.Id)
            .ToList();

        int[]? checkPoints = checkPointDetails.Count > 0
            ? checkPointDetails.Select(d => GetDetailInt(d) ?? 0).ToArray()
            : null;

        bool? checkPointStatus = checkPoints != null && checkPoints.Length > 0 ? checkPoints.All(x => x == 1) : null;
        string? ngBoxFinal = GetDetailText(FindDetail(parentDetails, "FINAL_INSPECTION", "NG_BOX_SENSOR_FINAL_INSPECTION_VALUE", "NG_BOX_SENSOR_FINAL_INSPECTION", "NG_BOX_FINAL", "NG_BOX")) ?? "ON";

        // 5. ECM Assy status check
        var ecmDetails = parentDetails
            .Where(d => string.Equals(d.Process?.Code, "ECM_ASSY", StringComparison.OrdinalIgnoreCase))
            .ToList();
        bool ecmStatus = (radCoreLabel == null || radCoreLabel == 1) &&
                         (motorFanLabel == null || motorFanLabel == 1) &&
                         (ecmDetails.Count == 0 || ecmDetails.All(d => d.Status));

        // 6. Final Inspection status check
        var finalDetails = parentDetails
            .Where(d => string.Equals(d.Process?.Code, "FINAL_INSPECTION", StringComparison.OrdinalIgnoreCase))
            .ToList();
        bool finalInspectionStatus = (finalRadCoreLabel == null || finalRadCoreLabel == 1) &&
                                     (checkPointStatus == null || checkPointStatus.Value) &&
                                     (finalDetails.Count == 0 || finalDetails.All(d => d.Status));

        // Overall status: Clinching, HE, M-Fan, ECM Assy, dan Final Inspection semuanya harus OK (true / 1).
        // Jika salah satu proses atau parameter bernilai false / NG (2) / Error (0), maka OverallStatus = false.
        var clinchingDetails = parentDetails
            .Where(d => string.Equals(d.Process?.Code, "CLINCHING_SHORT_SIDE", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(d.Process?.Code, "CLINCHING_LONG_SIDE", StringComparison.OrdinalIgnoreCase))
            .ToList();
        bool clinchingStatus = (oRingSet == null || oRingSet == 1) && 
                               (clinchingHeightStatus == null || clinchingHeightStatus.Value) &&
                               (endPlateStatus == null || endPlateStatus.Value) &&
                               (clinchingDetails.Count == 0 || clinchingDetails.All(d => d.Status));

        var heDetails = parentDetails
            .Where(d => string.Equals(d.Process?.Code, "HE_LEAK", StringComparison.OrdinalIgnoreCase))
            .ToList();
        bool heStatus = (capTypePos == null || capTypePos == 1) && 
                        (leakResult == null || leakResult == 1) &&
                        (heDetails.Count == 0 || heDetails.All(d => d.Status));

        bool mFanStatus = (mFanTest == null || mFanTest == 1) && 
                          (childDetails.Count == 0 || childDetails.All(d => d.Status)) &&
                          (childLog == null || childLog.Status);

        bool overallStatus = clinchingStatus &&
                             heStatus &&
                             mFanStatus && 
                             ecmStatus && 
                             finalInspectionStatus;

        return new ProcessLogMockDto
        {
            Id = log.Id,
            Timestamp = DateTimeHelper.FormatTimestamp(log.CreatedAt),
            SerialNumberClinching = clinchingSn,
            SerialNumberMFan = mFanSn,
            CoreAsmValue = coreAsm,
            UpperTankAsmValue = upperTank,
            LowerTankAsmValue = lowerTank,
            ORingSetResult = oRingSet,
            NgBoxSensorShortSideValue = ngBoxShort,
            ClinchingHeightValues = clinchingHeightValues,
            ClinchingHeightStatus = clinchingHeightStatus,
            EndPlateWidthResults = endPlateResults,
            EndPlateWidthStatus = endPlateStatus,
            NgBoxSensorLongSideValue = ngBoxLong,
            CapTypePositionResult = capTypePos,
            LeakResult = leakResult,
            LeakLastLeakageValue = leakValue,
            LotFanAsmResult = lotFan,
            LotMotorAsmResult = lotMotor,
            LotGuideAsmResult = lotGuide,
            BoltTightenValue = boltTighten,
            BoltTightenQtyValue = boltQty,
            NutTightenValue = nutTighten,
            MFanInspectionRotationSpeedMaxValue = rotMax,
            MFanInspectionRotationSpeedMinValue = rotMin,
            MFanInspectionAmpereMaxValue = ampMax,
            MFanInspectionAmpereMinValue = ampMin,
            MFanInspectionWindDirectionValue = windDir,
            MFanTestResult = mFanTest,
            NgBoxSensorMFanInspectionValue = ngBoxMFan,
            RadCoreAsmNameLabelResult = radCoreLabel,
            MotorFanAssyLabelResult = motorFanLabel,
            EcmAssyBoltTightenValue = ecmBolt,
            EcmAssyBoltTightenQtyValue = ecmBoltQty,
            NgBoxSensorEcmAssyValue = ngBoxEcm,
            FinalInspectionRadCoreAsmNameLabelResult = finalRadCoreLabel,
            CheckPoints = checkPoints,
            CheckPointStatus = checkPointStatus,
            NgBoxSensorFinalInspectionValue = ngBoxFinal,
            OverallStatus = overallStatus
        };
    }

    private static ProcessLogDetail? FindDetail(IEnumerable<ProcessLogDetail>? details, string? processCode, params string[] paramCodes)
    {
        if (details == null) return null;
        return details.FirstOrDefault(d =>
            (processCode == null || string.Equals(d.Process?.Code, processCode, StringComparison.OrdinalIgnoreCase)) &&
            paramCodes.Any(pc => string.Equals(d.Parameter?.Code, pc, StringComparison.OrdinalIgnoreCase)));
    }

    private static string? GetIssueNumberFromDetail(ProcessLogDetail? detail)
    {
        if (detail == null) return null;
        var text = detail.ValueText?.Trim();
        if (string.IsNullOrWhiteSpace(text)) return null;

        if (string.Equals(text, "OK", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(text, "NG", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(text, "TRUE", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(text, "FALSE", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(text, "PASSED", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(text, "REJECTED", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(text, "ON", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(text, "OFF", StringComparison.OrdinalIgnoreCase) ||
            text == "1" || text == "0")
        {
            return null;
        }

        return text;
    }

    private static string? GetDetailText(ProcessLogDetail? detail)
    {
        if (detail == null) return null;
        return detail.ValueText ?? detail.DisplayValue;
    }

    private static bool? GetDetailBool(ProcessLogDetail? detail)
    {
        if (detail == null) return null;
        if (detail.ValueBoolean.HasValue) return detail.ValueBoolean.Value;
        if (detail.ValueNumber.HasValue)
        {
            if (detail.ValueNumber.Value == 1) return true;
            if (detail.ValueNumber.Value == 0 || detail.ValueNumber.Value == 2) return false;
        }
        return detail.Status;
    }

    private static int? GetDetailInt(ProcessLogDetail? detail)
    {
        if (detail == null) return null;
        if (detail.ValueNumber.HasValue) return (int)detail.ValueNumber.Value;
        if (detail.ValueBoolean.HasValue) return detail.ValueBoolean.Value ? 1 : 2;
        if (!string.IsNullOrWhiteSpace(detail.ValueText))
        {
            var text = detail.ValueText.Trim().ToLowerInvariant();
            if (text == "1" || text == "ok" || text == "true" || text == "passed" || text == "on") return 1;
            if (text == "0" || text == "error" || text == "err" || text == "fail" || text == "failed") return 0;
            if (text == "2" || text == "ng" || text == "false" || text == "rejected" || text == "off") return 2;
            if (int.TryParse(detail.ValueText, out var val)) return val;
        }
        return detail.Status ? 1 : 2;
    }

    private static double? GetDetailNumber(ProcessLogDetail? detail)
    {
        if (detail == null) return null;
        if (detail.ValueNumber.HasValue) return (double)detail.ValueNumber.Value;
        return null;
    }

    public async Task<TraceabilityLog> CreateNewAsync(
        CreateProcessLogRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var clinchingSn = !string.IsNullOrWhiteSpace(request.SerialNumberClinching)
            ? request.SerialNumberClinching
            : request.SerialNumber ?? string.Empty;

        var mfanSn = !string.IsNullOrWhiteSpace(request.SerialNumberMFanAssy)
            ? request.SerialNumberMFanAssy
            : request.SerialNumberMFanAlternative;

        var processCode = !string.IsNullOrWhiteSpace(request.ProcessCode) ? request.ProcessCode : "ECM_ASSY";
        bool isFinish = string.Equals(processCode, "FINAL_INSPECTION", StringComparison.OrdinalIgnoreCase) || request.IsFInihed;

        var tracLog = await _traceabilityLogRepository.GetTraceabilityLogByCodeAsync(clinchingSn, cancellationToken);
        if (tracLog == null)
        {
            tracLog = new TraceabilityLog
            {
                Code = clinchingSn,
                SerialNumberClinching = !string.IsNullOrWhiteSpace(request.SerialNumberClinching) ? request.SerialNumberClinching : clinchingSn,
                SerialNumberMFan = mfanSn,
                Status = request.IsOk ?? true,
                IsFinish = isFinish,
                CreatedAt = request.Timestamp ?? DateTime.UtcNow
            };
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(request.SerialNumberClinching) && string.IsNullOrWhiteSpace(tracLog.SerialNumberClinching))
                tracLog.SerialNumberClinching = request.SerialNumberClinching;

            if (!string.IsNullOrWhiteSpace(mfanSn) && string.IsNullOrWhiteSpace(tracLog.SerialNumberMFan))
                tracLog.SerialNumberMFan = mfanSn;

            if (request.IsOk == false)
                tracLog.Status = false;

            if (isFinish)
                tracLog.IsFinish = true;

            tracLog.UpdatedAt = request.Timestamp ?? DateTime.UtcNow;
        }

        // 1. Process validation with its configured parameters
        var process = await _processRepository.GetByCodeWithParametersAsync(processCode, cancellationToken);

        if (process == null)
        {
            process = await _processRepository.FirstOrDefaultAsync(
                p => p.Code == processCode || p.Code.ToLower() == processCode.ToLower(), cancellationToken);
        }

        if (process == null)
        {
            process = new Process
            {
                Code = processCode,
                Name = processCode.Replace("_", " "),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            await _processRepository.AddAsync(process, cancellationToken);
            await _processRepository.SaveChangesAsync(cancellationToken);
        }

        // 2. Operator validation
        int? operatorId = null;
        if (!string.IsNullOrWhiteSpace(request.OperatorUsername))
        {
            var opUser = await _userRepository.FirstOrDefaultAsync(
                u => u.Username == request.OperatorUsername, cancellationToken);
            operatorId = opUser?.Id;
        }

        // 3. Dynamic parameter extraction from MQTT sub (request.Data)
        var mqttData = request.Data != null
            ? new Dictionary<string, object>(request.Data, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        // Take parameters associated with this process
        var processParameters = process.ProcessParameters?
            .Select(pp => pp.Parameter)
            .Where(p => p != null)
            .Cast<Parameter>()
            .ToList() ?? new List<Parameter>();

        // If process has no configured ProcessParameters yet in DB, look up parameters matching MQTT keys
        if (processParameters.Count == 0 && mqttData.Count > 0)
        {
            var keys = mqttData.Keys.ToList();
            processParameters = (await _parameterRepository.FindAsync(
                p => keys.Contains(p.Code), cancellationToken)).ToList();
        }

        foreach (var param in processParameters)
        {
            if (param == null) continue;

            string? strVal = null;
            if (mqttData.TryGetValue(param.Code, out var rawVal))
            {
                strVal = FormatParamValue(rawVal);
            }

            bool status = request.IsOk ?? true;

            var detail = new TraceabilityLogDetail
            {
                TraceabilityLogId = tracLog.Id,
                ProcessId = process.Id,
                ParameterId = param.Id,
                Value = strVal,
                Status = status,
                IsFinish = isFinish,
                OperatorId = operatorId,
                CreatedAt = request.Timestamp ?? DateTime.UtcNow
            };

            tracLog.Details.Add(detail);
        }

        // If there are additional parameters present in MQTT data not in ProcessParameters
        if (mqttData.Count > 0)
        {
            var processedParamIds = tracLog.Details.Select(d => d.ParameterId).ToHashSet();
            var remainingKeys = mqttData.Keys
                .Where(k => !processParameters.Any(p => string.Equals(p.Code, k, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (remainingKeys.Count > 0)
            {
                var extraParams = (await _parameterRepository.FindAsync(
                    p => remainingKeys.Contains(p.Code), cancellationToken)).ToList();

                foreach (var extraParam in extraParams)
                {
                    if (processedParamIds.Contains(extraParam.Id)) continue;

                    string? strVal = FormatParamValue(mqttData[extraParam.Code]);
                    bool status = request.IsOk ?? true;

                    var detail = new TraceabilityLogDetail
                    {
                        TraceabilityLogId = tracLog.Id,
                        ProcessId = process.Id,
                        ParameterId = extraParam.Id,
                        Value = strVal,
                        Status = status,
                        IsFinish = isFinish,
                        OperatorId = operatorId,
                        CreatedAt = request.Timestamp ?? DateTime.UtcNow
                    };

                    tracLog.Details.Add(detail);
                }
            }
        }

        return await _traceabilityLogRepository.CreateNewAsync(tracLog, cancellationToken);
    }


    private static string? FormatParamValue(object? val)
    {
        if (val == null) return null;
        if (val is System.Text.Json.JsonElement elem)
        {
            return elem.ValueKind switch
            {
                System.Text.Json.JsonValueKind.String => elem.GetString(),
                System.Text.Json.JsonValueKind.Number => elem.GetRawText(),
                System.Text.Json.JsonValueKind.True => "true",
                System.Text.Json.JsonValueKind.False => "false",
                System.Text.Json.JsonValueKind.Null => null,
                _ => elem.ToString()
            };
        }
        return val.ToString();
    }

    private static bool DetermineParamStatus(string? value, bool defaultStatus)
    {
        if (string.IsNullOrWhiteSpace(value)) return defaultStatus;
        var trimmed = value.Trim().ToUpperInvariant();
        if (trimmed == "0" || trimmed == "NG" || trimmed == "FALSE" || 
            trimmed == "REJECTED" || trimmed == "ERROR" || trimmed == "ERR" || trimmed == "FAIL" || trimmed == "FAILED")
        {
            return false;
        }
        return defaultStatus;
    }

    private static bool IsGroupItemFailed(string groupKey, object? val, bool? dbStatus)
    {
        if (val == null) return dbStatus == false;
        var s = val.ToString()?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(s)) return dbStatus == false;

        // Specific rules for CheckPoint: 0 = Error (Failed), 1 = OK (Passed), 2 = NG (Failed)
        if (string.Equals(groupKey, "CHECK_POINT", StringComparison.OrdinalIgnoreCase))
        {
            if (s == "1" || s == "OK" || s == "PASSED" || s == "TRUE")
            {
                return false; // OK
            }
            if (s == "0" || s == "2" || s == "NG" || s == "ERROR" || s == "ERR" || s == "FAIL" || s == "FAILED" || s == "FALSE" || s == "REJECTED")
            {
                return true; // Failed (Error or NG)
            }
            return dbStatus == false;
        }

        // For other groups (CLINCHING_HEIGHT, END_PLATE_WIDTH): continuous numeric measurements
        if (s == "0" || s == "NG" || s == "FALSE" || s == "REJECTED" || s == "FAIL" || s == "FAILED" || s == "ERR" || s == "ERROR")
        {
            return true;
        }

        if (s == "OK" || s == "PASSED" || s == "TRUE" || s == "ON")
        {
            return false;
        }

        if (decimal.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var num))
        {
            return num == 0;
        }

        return dbStatus == false;
    }

    public async Task<PagedResult<TraceabilityLogDto>> GetAllTraceabilityNewAsync(
        int page,
        int pageSize,
        string? search = null,
        bool? status = null,
        bool? isFinish = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var (items, totalCount) = await _traceabilityLogRepository.GetAllTraceabilityNewAsync(
            page, pageSize, search, status, isFinish, startDate, endDate, cancellationToken);

        var clinchingCodes = items.Select(x => x.SerialNumberClinching).Where(s => !string.IsNullOrWhiteSpace(s));
        var mfanCodes = items.Select(x => x.SerialNumberMFan).Where(s => !string.IsNullOrWhiteSpace(s));
        var allCodes = clinchingCodes.Concat(mfanCodes).Where(s => !string.IsNullOrWhiteSpace(s)).Cast<string>().Distinct().ToList();

        var issueMap = await _traceabilityLogRepository.GetIssueNumbersBySerialNumbersAsync(allCodes, cancellationToken);

        var dtos = items.Select(x =>
        {
            var clinchingIssues = (!string.IsNullOrWhiteSpace(x.SerialNumberClinching) && issueMap.TryGetValue(x.SerialNumberClinching, out var cIssues))
                ? cIssues
                : new List<string>();

            var mfanIssues = (!string.IsNullOrWhiteSpace(x.SerialNumberMFan) && issueMap.TryGetValue(x.SerialNumberMFan, out var mIssues))
                ? mIssues
                : new List<string>();

            return new TraceabilityLogDto
            {
                Id = x.Id,
                Code = x.Code,
                SerialNumberClinching = x.SerialNumberClinching,
                SerialNumberMFan = x.SerialNumberMFan,
                Status = x.Status,
                IsFinish = x.IsFinish,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                IssueNumbersClinching = clinchingIssues,
                IssueNumbersMfan = mfanIssues,
                Detail = null
            };
        }).ToList();

        return new PagedResult<TraceabilityLogDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<TraceabilityLogDto> GetBySerialNumberClinchingNewAsync(
        string serialNumberClinching,
        CancellationToken cancellationToken = default)
    {
        var x = await _traceabilityLogRepository.GetBySerialNumberClinchingAsync(serialNumberClinching, cancellationToken);
        if (x == null)
            throw new NotFoundException(nameof(TraceabilityLog), serialNumberClinching);

        var detailCodes = new[] { x.SerialNumberClinching, x.SerialNumberMFan }
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Cast<string>()
            .Distinct()
            .ToList();

        var issueMap = await _traceabilityLogRepository.GetIssueNumbersBySerialNumbersAsync(detailCodes, cancellationToken);

        var clinchingIssues = (!string.IsNullOrWhiteSpace(x.SerialNumberClinching) && issueMap.TryGetValue(x.SerialNumberClinching, out var cIssues))
            ? cIssues
            : new List<string>();

        var mfanIssues = (!string.IsNullOrWhiteSpace(x.SerialNumberMFan) && issueMap.TryGetValue(x.SerialNumberMFan, out var mIssues))
            ? mIssues
            : new List<string>();

        var clinchingSn = !string.IsNullOrWhiteSpace(x.SerialNumberClinching) ? x.SerialNumberClinching : serialNumberClinching;
        var processLog = await _traceabilityLogRepository.GetProcessLogFullValueAsync(clinchingSn, cancellationToken);

        // 1. Dynamic Clinching process parameters from ProcessLog (or fallback to TraceabilityLogDetails)
        var clinchingProcessDetails = processLog?.Details?
            .Where(d => d.Process != null &&
                        (d.Process.Code.Contains("CLINCHING", StringComparison.OrdinalIgnoreCase) ||
                         d.Process.Code.Contains("HE_LEAK", StringComparison.OrdinalIgnoreCase) ||
                         d.Process.Code.Contains("LEAK", StringComparison.OrdinalIgnoreCase)))
            .ToList();

        var clinchingList = (clinchingProcessDetails != null && clinchingProcessDetails.Count > 0)
            ? TraceabilityLogMapper.MapProcessLogDetailsToParameterDtos(clinchingProcessDetails, clinchingIssues)
            : TraceabilityLogMapper.MapTraceabilityLogDetailsToParameterDtos((x.Details ?? Enumerable.Empty<TraceabilityLogDetail>())
                .Where(d => d.Process != null && (d.Process.Code.Contains("CLINCHING", StringComparison.OrdinalIgnoreCase) ||
                                                  d.Process.Code.Contains("HE_LEAK", StringComparison.OrdinalIgnoreCase) ||
                                                  d.Process.Code.Contains("LEAK", StringComparison.OrdinalIgnoreCase))));

        // 2. Dynamic M-Fan process parameters from ProcessLog of SerialNumberMFan (or fallback to TraceabilityLogDetails)
        var mfanSn = x.SerialNumberMFan;
        var mfanProcessLog = !string.IsNullOrWhiteSpace(mfanSn)
            ? await _traceabilityLogRepository.GetProcessLogFullValueAsync(mfanSn, cancellationToken)
            : null;

        var mfanProcessDetails = mfanProcessLog?.Details?
            .Where(d => d.Process != null &&
                        (d.Process.Code.Contains("M_FAN", StringComparison.OrdinalIgnoreCase) ||
                         d.Process.Code.Contains("FAN", StringComparison.OrdinalIgnoreCase)))
            .ToList();

        var mfanList = (mfanProcessDetails != null && mfanProcessDetails.Count > 0)
            ? TraceabilityLogMapper.MapProcessLogDetailsToParameterDtos(mfanProcessDetails, mfanIssues)
            : TraceabilityLogMapper.MapTraceabilityLogDetailsToParameterDtos((x.Details ?? Enumerable.Empty<TraceabilityLogDetail>())
                .Where(d => d.Process != null && (d.Process.Code.Contains("M_FAN", StringComparison.OrdinalIgnoreCase) ||
                                                  d.Process.Code.Contains("FAN", StringComparison.OrdinalIgnoreCase))));

        // 3. Dynamic ECM parameters
        var ecmList = TraceabilityLogMapper.MapTraceabilityLogDetailsToParameterDtos((x.Details ?? Enumerable.Empty<TraceabilityLogDetail>())
            .Where(d => (d.Process != null && (d.Process.Code.Contains("ECM", StringComparison.OrdinalIgnoreCase) ||
                                                d.Process.Code.Equals("ECM_ASSY", StringComparison.OrdinalIgnoreCase))) ||
                        (d.Parameter != null && (d.Parameter.Code.Contains("ECM", StringComparison.OrdinalIgnoreCase) ||
                                                 d.Parameter.Code.Contains("MOTOR_FAN", StringComparison.OrdinalIgnoreCase) ||
                                                 d.Parameter.Code.StartsWith("RAD_CORE", StringComparison.OrdinalIgnoreCase)))));

        // 4. Dynamic Final Inspection parameters
        var finalList = TraceabilityLogMapper.MapTraceabilityLogDetailsToParameterDtos((x.Details ?? Enumerable.Empty<TraceabilityLogDetail>())
            .Where(d => (d.Process != null && (d.Process.Code.Contains("FINAL", StringComparison.OrdinalIgnoreCase) ||
                                                d.Process.Code.Equals("FINAL_INSPECTION", StringComparison.OrdinalIgnoreCase))) ||
                        (d.Parameter != null && (d.Parameter.Code.Contains("FINAL", StringComparison.OrdinalIgnoreCase) ||
                                                 d.Parameter.Code.StartsWith("CHECK_POINT", StringComparison.OrdinalIgnoreCase)))));

        return TraceabilityLogMapper.MapToTraceabilityLogDto(
            x, clinchingIssues, mfanIssues, clinchingList, mfanList, ecmList, finalList);
    }

    public static List<TraceabilityLogParameterDto> MapProcessLogDetailsToParameterDtos(
        IEnumerable<ProcessLogDetail>? details,
        IEnumerable<string>? fallbackIssues = null)
        => TraceabilityLogMapper.MapProcessLogDetailsToParameterDtos(details, fallbackIssues);

    public static List<TraceabilityLogParameterDto> MapTraceabilityLogDetailsToParameterDtos(
        IEnumerable<TraceabilityLogDetail>? details)
        => TraceabilityLogMapper.MapTraceabilityLogDetailsToParameterDtos(details);

    public async Task<List<TraceabilityLogDto>> GetRecentTraceabilityLogsAsync(
        int count = 10,
        CancellationToken cancellationToken = default)
    {
        var items = (await _traceabilityLogRepository.GetRecentTraceabilityLogsAsync(count, cancellationToken)).ToList();

        var clinchingCodes = items.Select(x => x.SerialNumberClinching).Where(s => !string.IsNullOrWhiteSpace(s));
        var mfanCodes = items.Select(x => x.SerialNumberMFan).Where(s => !string.IsNullOrWhiteSpace(s));
        var allCodes = clinchingCodes.Concat(mfanCodes).Where(s => !string.IsNullOrWhiteSpace(s)).Cast<string>().Distinct().ToList();

        var issueMap = await _traceabilityLogRepository.GetIssueNumbersBySerialNumbersAsync(allCodes, cancellationToken);

        var dtos = items.Select(x =>
        {
            var clinchingIssues = (!string.IsNullOrWhiteSpace(x.SerialNumberClinching) && issueMap.TryGetValue(x.SerialNumberClinching, out var cIssues))
                ? cIssues
                : new List<string>();

            var mfanIssues = (!string.IsNullOrWhiteSpace(x.SerialNumberMFan) && issueMap.TryGetValue(x.SerialNumberMFan, out var mIssues))
                ? mIssues
                : new List<string>();

            return new TraceabilityLogDto
            {
                Id = x.Id,
                Code = x.Code,
                SerialNumberClinching = x.SerialNumberClinching,
                SerialNumberMFan = x.SerialNumberMFan,
                Status = x.Status,
                IsFinish = x.IsFinish,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                IssueNumbersClinching = clinchingIssues,
                IssueNumbersMfan = mfanIssues,
                Detail = null
            };
        }).ToList();

        return dtos;
    }

    public async Task<TraceabilityLogIssueDto> GetIssuesBySerialNumberAsync(
        string serialNumber,
        bool status = false,
        CancellationToken cancellationToken = default)
    {
        // Validasi: cek actual status dari serial number clinching
        // Jika actual status tidak sesuai dengan parameter yang dikirim, return empty
        var checkSerialNumber = await _traceabilityLogRepository.GetBySerialNumberClinchingAsync(serialNumber, cancellationToken);
        if (checkSerialNumber == null || checkSerialNumber.Status != status)
        {
            throw new NotFoundException(nameof(TraceabilityLog), serialNumber);
        }

        var issues = await _traceabilityLogRepository.GetIssuesBySerialNumberAsync(
            serialNumber, status, isFinish: true, cancellationToken);

        return new TraceabilityLogIssueDto
        {
            SerialNumber = serialNumber,
            Issues = issues.Select(i => new TraceabilityLogIssueItemDto
            {
                IssueNumber = i.IssueNumber,
                PartNumber = i.PartNumber,
                PartName = i.PartName
            }).ToList()
        };
    }
}

