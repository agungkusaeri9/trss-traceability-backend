using Mapster;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TraceabilitySystem.Application.DTOs.Pagination;
using TraceabilitySystem.Application.DTOs.ProcessLog;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Domain.Entities;
using TraceabilitySystem.Domain.Interfaces;
using TraceabilitySystem.Shared.Exceptions;
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

    public TraceabilityLogService(ITraceabilityLogRepository traceabilityLogRepository)
    {
        _traceabilityLogRepository = traceabilityLogRepository;
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

        var result = new ProcessLogFullValueDto
        {
            Id = log.Id,
            SerialNumberCode = log.SerialNumber.SerialNumberCode,
            Status = log.Status,
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
                .Where(x => !OverallProcessCodes.Contains(x.Process.Code))
                .OrderBy(x => string.Equals(x.Process.Code, "HE_LEAK", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenBy(x => x.Process.Order)
                .ThenBy(x => x.Parameter.Order)
                .Adapt<List<ProcessLogFullValueDetailDto>>()
        };

        SetOrAddDetail(result.Clinching.Details, "CLINCHING_SHORT_SIDE", "Clinching Short Side", "CORE_ASM_VALUE", "Core Asm", coreAsm);
        SetOrAddDetail(result.Clinching.Details, "CLINCHING_SHORT_SIDE", "Clinching Short Side", "UPPER_TANK_ASM_VALUE", "Upper Tank Asm", upperTank);
        SetOrAddDetail(result.Clinching.Details, "CLINCHING_SHORT_SIDE", "Clinching Short Side", "LOWER_TANK_ASM_VALUE", "Lower Tank Asm", lowerTank);

        var childRel = log.SerialNumber.ParentRelations?.FirstOrDefault();
        var childSn = childRel?.ChildSerialNumber;
        var childLog = childSn?.ProcessLogs?.OrderByDescending(x => x.CreatedAt).FirstOrDefault();
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
                    .OrderBy(x => x.Process.Order)
                    .ThenBy(x => x.Parameter.Order)
                    .Adapt<List<ProcessLogFullValueDetailDto>>()
            };

            SetOrAddDetail(result.MFan.Details, "M_FAN_ASSY", "M-Fan Assembly", "LOT_FAN_ASM_RESULT", "Lot Fan Asm", lotFan);
            SetOrAddDetail(result.MFan.Details, "M_FAN_ASSY", "M-Fan Assembly", "LOT_MOTOR_ASM_RESULT", "Lot Motor Asm", lotMotor);
            SetOrAddDetail(result.MFan.Details, "M_FAN_ASSY", "M-Fan Assembly", "LOT_GUIDE_ASM_RESULT", "Lot Guide Asm", lotGuide);
        }

        result.Overall = log.Details
            .Where(x => OverallProcessCodes.Contains(x.Process.Code))
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

        bool oRingSet = GetDetailBool(FindDetail(parentDetails, "CLINCHING_SHORT_SIDE", "O_RING_SET_RESULT", "O_RING_SET", "ORING_SET_RESULT")) ?? true;
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
                .Where(v => v > 0)
                .ToArray();
        }
        else
        {
            clinchingHeightValues = Array.Empty<double>();
        }

        var avgDetail = GetDetailNumber(FindDetail(parentDetails, "CLINCHING_LONG_SIDE", "CLINCHING_HEIGHT_AVERAGE", "CLINCHING_HEIGHT_AVG", "CLINCHING_AVG"));
        double clinchingAvg = avgDetail ?? (clinchingHeightValues.Length > 0 ? Math.Round(clinchingHeightValues.Average(), 2) : 0.0);

        var endPlateDetails = parentDetails
            .Where(d => string.Equals(d.Process?.Code, "CLINCHING_LONG_SIDE", StringComparison.OrdinalIgnoreCase) &&
                        (d.Parameter?.Code?.StartsWith("END_PLATE_WIDTH", StringComparison.OrdinalIgnoreCase) == true ||
                         d.Parameter?.Name?.Contains("End Plate", StringComparison.OrdinalIgnoreCase) == true))
            .OrderBy(d => d.Parameter?.Order ?? d.Id)
            .ToList();

        bool[] endPlateResults = endPlateDetails.Select(d => GetDetailBool(d) ?? true).ToArray();
        bool endPlateStatus = endPlateResults.Length > 0 ? endPlateResults.All(x => x) : true;
        string ngBoxLong = GetDetailText(FindDetail(parentDetails, "CLINCHING_LONG_SIDE", "NG_BOX_SENSOR_LONG_SIDE_VALUE", "NG_BOX_SENSOR_LONG_SIDE", "NG_BOX_LONG_SIDE", "NG_BOX")) ?? "ON";

        // 2.5. HE Leak
        bool? capTypePos = GetDetailBool(FindDetail(parentDetails, "HE_LEAK", "CAP_TYPE_POSITION_RESULT", "CAP_TYPE_POSITION", "CAP_TYPE"));
        bool? leakResult = GetDetailBool(FindDetail(parentDetails, "HE_LEAK", "LEAK_RESULT", "LEAK_TEST_RESULT", "LEAK_STATUS"));
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
        bool? nutTighten = GetDetailBool(FindDetail(childDetails, "M_FAN_ASSY", "NUT_TIGHTEN_VALUE", "NUT_TIGHTEN"));

        // 4. M-Fan Inspection (Comprehensive alias mapping)
        double? rotMax = GetDetailNumber(FindDetail(childDetails, "M_FAN_INSPECTION", "M_FAN_INSPECTION_ROTATION_SPEED_MAX_VALUE", "M_FAN_INSPECTION_ROTATION_SPEED_MAX", "ROTATION_SPEED_MAX", "ROT_MAX"));
        double? rotMin = GetDetailNumber(FindDetail(childDetails, "M_FAN_INSPECTION", "M_FAN_INSPECTION_ROTATION_SPEED_MIN_VALUE", "M_FAN_INSPECTION_ROTATION_SPEED_MIN", "ROTATION_SPEED_MIN", "ROT_MIN"));
        double? ampMax = GetDetailNumber(FindDetail(childDetails, "M_FAN_INSPECTION", "M_FAN_INSPECTION_AMPERE_MAX_VALUE", "M_FAN_INSPECTION_AMPERE_MAX", "AMPERE_MAX", "AMP_MAX"));
        double? ampMin = GetDetailNumber(FindDetail(childDetails, "M_FAN_INSPECTION", "M_FAN_INSPECTION_AMPERE_MIN_VALUE", "M_FAN_INSPECTION_AMPERE_MIN", "AMPERE_MIN", "AMP_MIN"));
        string? windDir = GetDetailText(FindDetail(childDetails, "M_FAN_INSPECTION", "M_FAN_INSPECTION_WIND_DIRECTION_VALUE", "M_FAN_INSPECTION_WIND_DIRECTION", "WIND_DIRECTION"));
        bool? mFanTest = GetDetailBool(FindDetail(childDetails, "M_FAN_INSPECTION", "M_FAN_TEST_RESULT", "MFAN_TEST_RESULT", "TEST_RESULT"));
        string? ngBoxMFan = GetDetailText(FindDetail(childDetails, "M_FAN_INSPECTION", "NG_BOX_SENSOR_M_FAN_INSPECTION_VALUE", "NG_BOX_SENSOR_M_FAN_INSPECTION", "NG_BOX_MFAN", "NG_BOX")) ?? "ON";

        // 5. ECM Assy
        bool? radCoreLabel = GetDetailBool(FindDetail(parentDetails, "ECM_ASSY", "RAD_CORE_ASM_NAME_LABEL_RESULT", "RAD_CORE_LABEL", "RAD_CORE_ASM_LABEL"));
        bool? motorFanLabel = GetDetailBool(FindDetail(parentDetails, "ECM_ASSY", "MOTOR_FAN_ASSY_LABEL_RESULT", "MOTOR_FAN_LABEL", "MOTOR_FAN_ASSY_LABEL"));
        double? ecmBolt = GetDetailNumber(FindDetail(parentDetails, "ECM_ASSY", "ECM_ASSY_BOLT_TIGHTEN_VALUE", "ECM_BOLT_TIGHTEN", "ECM_BOLT"));
        double? ecmBoltQty = GetDetailNumber(FindDetail(parentDetails, "ECM_ASSY", "ECM_ASSY_BOLT_TIGHTEN_QTY_VALUE", "ECM_BOLT_QTY", "ECM_ASSY_BOLT_QTY"));
        string? ngBoxEcm = GetDetailText(FindDetail(parentDetails, "ECM_ASSY", "NG_BOX_SENSOR_ECM_ASSY_VALUE", "NG_BOX_SENSOR_ECM_ASSY", "NG_BOX_ECM", "NG_BOX")) ?? "ON";

        // 6. Final Inspection
        bool? finalRadCoreLabel = GetDetailBool(FindDetail(parentDetails, "FINAL_INSPECTION", "FINAL_INSPECTION_RAD_CORE_ASM_NAME_LABEL_RESULT", "FINAL_RAD_CORE_LABEL"));

        var checkPointDetails = parentDetails
            .Where(d => string.Equals(d.Process?.Code, "FINAL_INSPECTION", StringComparison.OrdinalIgnoreCase) &&
                        (d.Parameter?.Code?.StartsWith("CHECK_POINT", StringComparison.OrdinalIgnoreCase) == true ||
                         d.Parameter?.Name?.Contains("Check Point", StringComparison.OrdinalIgnoreCase) == true))
            .OrderBy(d => d.Parameter?.Order ?? d.Id)
            .ToList();

        bool[]? checkPoints = checkPointDetails.Count > 0
            ? checkPointDetails.Select(d => GetDetailBool(d) ?? true).ToArray()
            : null;

        bool? checkPointStatus = checkPoints != null && checkPoints.Length > 0 ? checkPoints.All(x => x) : null;
        string? ngBoxFinal = GetDetailText(FindDetail(parentDetails, "FINAL_INSPECTION", "NG_BOX_SENSOR_FINAL_INSPECTION_VALUE", "NG_BOX_SENSOR_FINAL_INSPECTION", "NG_BOX_FINAL", "NG_BOX")) ?? "ON";

        // Overall status: Harus log.Status == true dan seluruh Checkpoint Final Inspection bernilai true
        bool overallStatus = log.Status && (checkPointStatus == null || checkPointStatus.Value);

        return new ProcessLogMockDto
        {
            Id = log.Id,
            Timestamp = log.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
            SerialNumberClinching = clinchingSn,
            SerialNumberMFan = mFanSn,
            CoreAsmValue = coreAsm,
            UpperTankAsmValue = upperTank,
            LowerTankAsmValue = lowerTank,
            ORingSetResult = oRingSet,
            NgBoxSensorShortSideValue = ngBoxShort,
            ClinchingHeightValues = clinchingHeightValues,
            ClinchingHeightAverage = clinchingAvg,
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
        return !string.IsNullOrWhiteSpace(detail.ValueText) ? detail.ValueText :
               detail.ValueNumber.HasValue ? detail.ValueNumber.Value.ToString(CultureInfo.InvariantCulture) :
               detail.ValueBoolean.HasValue ? (detail.ValueBoolean.Value ? "OK" : "NG") :
               detail.DisplayValue;
    }

    private static bool? GetDetailBool(ProcessLogDetail? detail)
    {
        if (detail == null) return null;
        if (detail.ValueBoolean.HasValue) return detail.ValueBoolean.Value;
        if (!string.IsNullOrWhiteSpace(detail.ValueText))
        {
            if (string.Equals(detail.ValueText, "OK", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(detail.ValueText, "true", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(detail.ValueText, "ON", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(detail.ValueText, "PASSED", StringComparison.OrdinalIgnoreCase))
                return true;
            if (string.Equals(detail.ValueText, "NG", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(detail.ValueText, "false", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(detail.ValueText, "OFF", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(detail.ValueText, "REJECTED", StringComparison.OrdinalIgnoreCase))
                return false;
        }
        return detail.Status;
    }

    private static double? GetDetailNumber(ProcessLogDetail? detail)
    {
        if (detail == null) return null;
        if (detail.ValueNumber.HasValue) return (double)detail.ValueNumber.Value;
        if (!string.IsNullOrWhiteSpace(detail.ValueText) && double.TryParse(detail.ValueText, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
            return parsed;
        return null;
    }
}
