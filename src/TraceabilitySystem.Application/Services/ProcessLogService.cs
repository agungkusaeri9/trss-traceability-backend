using System.Globalization;
using Mapster;
using TraceabilitySystem.Application.DTOs.ProcessLog;
using TraceabilitySystem.Application.DTOs.SerialNumber;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Domain.Entities;
using TraceabilitySystem.Domain.Interfaces;
using TraceabilitySystem.Shared.Exceptions;
using TraceabilitySystem.Shared.Helpers;
using TraceabilitySystem.Shared.Models;

namespace TraceabilitySystem.Application.Services;

public class ProcessLogService : IProcessLogService
{
    private readonly IProcessLogRepository _processLogRepository;
    private readonly IIssueRepository _issueRepository;
    private readonly IStockInRepository _stockInRepository;
    private readonly ISerialNumberRepository _serialNumberRepository;
    private readonly IParameterRepository _parameterRepository;
    private readonly IProcessRepository _processRepository;
    private readonly ISerialNumberService _serialNumberService;
    private readonly IMqttPublisher _mqttPublisher;
    private readonly IPrintService _printService;

    public ProcessLogService(
        IProcessLogRepository processLogRepository,
        IIssueRepository issueRepository,
        IStockInRepository stockInRepository,
        ISerialNumberRepository serialNumberRepository,
        IParameterRepository parameterRepository,
        IProcessRepository processRepository,
        ISerialNumberService serialNumberService,
        IMqttPublisher mqttPublisher,
        IPrintService printService)
    {
        _processLogRepository = processLogRepository;
        _issueRepository = issueRepository;
        _stockInRepository = stockInRepository;
        _serialNumberRepository = serialNumberRepository;
        _parameterRepository = parameterRepository;
        _processRepository = processRepository;
        _serialNumberService = serialNumberService;
        _mqttPublisher = mqttPublisher;
        _printService = printService;
    }

    public async Task<PagedResult<ProcessLogListDto>> GetProcessLogsAsync(
        int page,
        int pageSize,
        string? serialNumberCode = null,
        bool? status = null,
        bool? isFinished = null,
        CancellationToken cancellationToken = default)
    {
        var (logs, totalCount) = await _processLogRepository.GetPagedLogsAsync(
            page, pageSize, serialNumberCode, status, isFinished, clinchingOnly: false, cancellationToken: cancellationToken);

        var dtos = logs.Select(log => MapToListDto(log)).ToList();

        return new PagedResult<ProcessLogListDto>
        {
            Items      = dtos,
            TotalCount = totalCount,
            Page       = page,
            PageSize   = pageSize
        };
    }

    public async Task<PagedResult<ProcessLogMockDto>> GetTraceabilityLogsAsync(
        int page,
        int pageSize,
        string? serialNumberCode = null,
        bool? status = null,
        bool? isFinished = null,
        CancellationToken cancellationToken = default)
    {
        var (logs, totalCount) = await _processLogRepository.GetPagedLogsAsync(
            page, pageSize, serialNumberCode, status, isFinished, clinchingOnly: true, cancellationToken: cancellationToken);

        var dtos = logs.Select(log => MapToMockDto(log)).ToList();

        return new PagedResult<ProcessLogMockDto>
        {
            Items      = dtos,
            TotalCount = totalCount,
            Page       = page,
            PageSize   = pageSize
        };
    }

    public async Task<ProcessLogMockDto> GetTraceabilityLogByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var log = await _processLogRepository.GetLogWithDetailsAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(ProcessLog), id);

        return MapToMockDto(log);
    }

    public async Task<ProcessLogMockDto> GetTraceabilityLogBySerialNumberAsync(string serialNumber, CancellationToken cancellationToken = default)
    {
        var log = await _processLogRepository.GetLogBySerialNumberAsync(serialNumber, cancellationToken)
            ?? throw new NotFoundException(nameof(ProcessLog), serialNumber);

        return MapToMockDto(log);
    }

    public async Task<ProcessLogDto> GetProcessLogByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var log = await _processLogRepository.GetLogWithDetailsAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(ProcessLog), id);

        return MapToDto(log);
    }

    private static readonly HashSet<string> OverallProcessCodes =
[
    "ECM_ASSY",
    "FINAL_INSPECTION"
];

    public async Task<ProcessLogFullValueDto> GetProcessLogFullValuesAsync(
    string serialNumberCode,
    CancellationToken cancellationToken = default)
    {
        var log = await _processLogRepository.GetProcessLogFullValueAsync(serialNumberCode, cancellationToken)
            ?? throw new NotFoundException(nameof(ProcessLog), serialNumberCode);

        var result = log.Adapt<ProcessLogFullValueDto>();

        result.Clinching = new ProcessLogFullValueParentDto
        {
            SerialNumberCode = log.SerialNumber.SerialNumberCode,
            Details = log.Details
                .Where(x => !OverallProcessCodes.Contains(x.Process.Code))
                .OrderBy(x => x.Process.Order)
                .ThenBy(x => x.Parameter.Order)
                .Adapt<List<ProcessLogFullValueDetailDto>>()
        };

        var childLog = log.SerialNumber.ParentRelations
            .SelectMany(x => x.ChildSerialNumber.ProcessLogs)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefault();

        if (childLog != null)
        {
            result.MFan = new ProcessLogFullValueChildDto
            {
                SerialNumberCode = childLog.SerialNumber.SerialNumberCode,
                Details = childLog.Details
                    .OrderBy(x => x.Process.Order)
                    .ThenBy(x => x.Parameter.Order)
                    .Adapt<List<ProcessLogFullValueDetailDto>>()
            };
        }

        result.Overall = log.Details
            .Where(x => OverallProcessCodes.Contains(x.Process.Code))
            .OrderBy(x => x.Process.Order)
            .ThenBy(x => x.Parameter.Order)
            .Adapt<List<ProcessLogFullValueDetailDto>>();

        return result;
    }

    public async Task<ProcessLogDto> GetProcessLogBySerialNumberAsync(string serialNumber, CancellationToken cancellationToken = default)
    {
        var log = await _processLogRepository.GetLogBySerialNumberAsync(serialNumber, cancellationToken);
        if (log == null) throw new NotFoundException(nameof(ProcessLog), serialNumber);

        return MapToDto(log);
    }

    // ── Private helpers ─────────────────────────────────────────────────────────

    private static ProcessLogDetail? FindDetail(IEnumerable<ProcessLogDetail>? details, string? processCode, params string[] paramCodes)
    {
        if (details == null) return null;
        return details.FirstOrDefault(d =>
            (processCode == null || string.Equals(d.Process?.Code, processCode, StringComparison.OrdinalIgnoreCase)) &&
            paramCodes.Any(pc => string.Equals(d.Parameter?.Code, pc, StringComparison.OrdinalIgnoreCase)));
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

    public static ProcessLogClinchingDetailDto MapClinchingDetail(ProcessLog log)
    {
        var sn = log.SerialNumber;
        var clinchingSn = sn?.SerialNumberCode ?? string.Empty;
        var parentDetails = log.Details ?? new List<ProcessLogDetail>();

        var parentIssues = sn?.Issues?
            .Where(x => x.Issue != null)
            .OrderBy(x => x.CreatedAt)
            .ToList() ?? new List<SerialNumberIssue>();

        string coreAsm = GetDetailText(FindDetail(parentDetails, "CLINCHING_SHORT_SIDE", "CORE_ASM_RESULT", "CORE_ASM"))
            ?? parentIssues.FirstOrDefault(i => i.Issue?.StockIn?.Part?.Name?.Contains("Core", StringComparison.OrdinalIgnoreCase) == true)?.Issue?.Number
            ?? (parentIssues.Count > 0 ? parentIssues[0].Issue?.Number : null)
            ?? string.Empty;

        string upperTank = GetDetailText(FindDetail(parentDetails, "CLINCHING_SHORT_SIDE", "UPPER_TANK_ASM_RESULT", "UPPER_TANK_ASM"))
            ?? parentIssues.FirstOrDefault(i => i.Issue?.StockIn?.Part?.Name?.Contains("Upper", StringComparison.OrdinalIgnoreCase) == true)?.Issue?.Number
            ?? (parentIssues.Count > 1 ? parentIssues[1].Issue?.Number : null)
            ?? string.Empty;

        string lowerTank = GetDetailText(FindDetail(parentDetails, "CLINCHING_SHORT_SIDE", "LOWER_TANK_ASM_RESULT", "LOWER_TANK_ASM"))
            ?? parentIssues.FirstOrDefault(i => i.Issue?.StockIn?.Part?.Name?.Contains("Lower", StringComparison.OrdinalIgnoreCase) == true)?.Issue?.Number
            ?? (parentIssues.Count > 2 ? parentIssues[2].Issue?.Number : null)
            ?? string.Empty;

        bool oRingSet = GetDetailBool(FindDetail(parentDetails, "CLINCHING_SHORT_SIDE", "O_RING_SET_RESULT", "O_RING_SET", "ORING_SET_RESULT")) ?? true;
        string ngBoxShort = GetDetailText(FindDetail(parentDetails, "CLINCHING_SHORT_SIDE", "NG_BOX_SENSOR_SHORT_SIDE", "NG_BOX_SHORT_SIDE", "NG_BOX")) ?? "ON";

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
        string ngBoxLong = GetDetailText(FindDetail(parentDetails, "CLINCHING_LONG_SIDE", "NG_BOX_SENSOR_LONG_SIDE", "NG_BOX_LONG_SIDE", "NG_BOX")) ?? "ON";

        return new ProcessLogClinchingDetailDto
        {
            SerialNumberClinching = clinchingSn,
            CoreAsmValue = coreAsm,
            UpperTankAsmValue = upperTank,
            LowerTankAsmValue = lowerTank,
            ORingSetResult = oRingSet,
            NgBoxSensorShortSideValue = ngBoxShort,
            ClinchingHeightValues = clinchingHeightValues,
            ClinchingHeightAverage = clinchingAvg,
            EndPlateWidthResults = endPlateResults,
            EndPlateWidthStatus = endPlateStatus,
            NgBoxSensorLongSideValue = ngBoxLong
        };
    }

    public static ProcessLogMFanDetailDto MapMFanDetail(ProcessLog log)
    {
        var sn = log.SerialNumber;
        var mFanSn = sn?.SerialNumberCode;
        var details = log.Details ?? new List<ProcessLogDetail>();

        var issues = sn?.Issues?
            .Where(x => x.Issue != null)
            .OrderBy(x => x.CreatedAt)
            .ToList() ?? new List<SerialNumberIssue>();

        string? lotFan = GetDetailText(FindDetail(details, "M_FAN_ASSY", "LOT_FAN_ASM_RESULT", "LOT_FAN_ASM", "FAN_ASM"))
            ?? issues.FirstOrDefault(i => i.Issue?.StockIn?.Part?.Name?.Contains("Fan", StringComparison.OrdinalIgnoreCase) == true)?.Issue?.Number
            ?? (issues.Count > 0 ? issues[0].Issue?.Number : null);

        string? lotMotor = GetDetailText(FindDetail(details, "M_FAN_ASSY", "LOT_MOTOR_ASM_RESULT", "LOT_MOTOR_ASM", "MOTOR_ASM"))
            ?? issues.FirstOrDefault(i => i.Issue?.StockIn?.Part?.Name?.Contains("Motor", StringComparison.OrdinalIgnoreCase) == true)?.Issue?.Number
            ?? (issues.Count > 1 ? issues[1].Issue?.Number : null);

        string? lotGuide = GetDetailText(FindDetail(details, "M_FAN_ASSY", "LOT_GUIDE_ASM_RESULT", "LOT_GUIDE_ASM", "GUIDE_ASM"))
            ?? issues.FirstOrDefault(i => i.Issue?.StockIn?.Part?.Name?.Contains("Guide", StringComparison.OrdinalIgnoreCase) == true)?.Issue?.Number
            ?? (issues.Count > 2 ? issues[2].Issue?.Number : null);

        string? boltTighten = GetDetailText(FindDetail(details, "M_FAN_ASSY", "BOLT_TIGHTEN_VALUE", "BOLT_TIGHTEN"));
        string? boltQty = GetDetailText(FindDetail(details, "M_FAN_ASSY", "BOLT_TIGHTEN_QTY_VALUE", "BOLT_TIGHTEN_QTY", "BOLT_QTY"));
        bool? nutTighten = GetDetailBool(FindDetail(details, "M_FAN_ASSY", "NUT_TIGHTEN_VALUE", "NUT_TIGHTEN"));

        double? rotMax = GetDetailNumber(FindDetail(details, "M_FAN_INSPECTION", "M_FAN_INSPECTION_ROTATION_SPEED_MAX", "ROTATION_SPEED_MAX", "ROT_MAX"));
        double? rotMin = GetDetailNumber(FindDetail(details, "M_FAN_INSPECTION", "M_FAN_INSPECTION_ROTATION_SPEED_MIN", "ROTATION_SPEED_MIN", "ROT_MIN"));
        double? ampMax = GetDetailNumber(FindDetail(details, "M_FAN_INSPECTION", "M_FAN_INSPECTION_AMPERE_MAX", "AMPERE_MAX", "AMP_MAX"));
        double? ampMin = GetDetailNumber(FindDetail(details, "M_FAN_INSPECTION", "M_FAN_INSPECTION_AMPERE_MIN", "AMPERE_MIN", "AMP_MIN"));
        string? windDir = GetDetailText(FindDetail(details, "M_FAN_INSPECTION", "M_FAN_INSPECTION_WIND_DIRECTION", "WIND_DIRECTION"));
        bool? mFanTest = GetDetailBool(FindDetail(details, "M_FAN_INSPECTION", "M_FAN_TEST_RESULT", "MFAN_TEST_RESULT", "TEST_RESULT"));
        string? ngBoxMFan = GetDetailText(FindDetail(details, "M_FAN_INSPECTION", "NG_BOX_SENSOR_M_FAN_INSPECTION", "NG_BOX_MFAN", "NG_BOX")) ?? "ON";

        return new ProcessLogMFanDetailDto
        {
            SerialNumberMFan = mFanSn,
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
            NgBoxSensorMFanInspectionValue = ngBoxMFan
        };
    }

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

        string coreAsm = GetDetailText(FindDetail(parentDetails, "CLINCHING_SHORT_SIDE", "CORE_ASM_RESULT", "CORE_ASM"))
            ?? parentIssues.FirstOrDefault(i => i.Issue?.StockIn?.Part?.Name?.Contains("Core", StringComparison.OrdinalIgnoreCase) == true)?.Issue?.Number
            ?? (parentIssues.Count > 0 ? parentIssues[0].Issue?.Number : null)
            ?? string.Empty;

        string upperTank = GetDetailText(FindDetail(parentDetails, "CLINCHING_SHORT_SIDE", "UPPER_TANK_ASM_RESULT", "UPPER_TANK_ASM"))
            ?? parentIssues.FirstOrDefault(i => i.Issue?.StockIn?.Part?.Name?.Contains("Upper", StringComparison.OrdinalIgnoreCase) == true)?.Issue?.Number
            ?? (parentIssues.Count > 1 ? parentIssues[1].Issue?.Number : null)
            ?? string.Empty;

        string lowerTank = GetDetailText(FindDetail(parentDetails, "CLINCHING_SHORT_SIDE", "LOWER_TANK_ASM_RESULT", "LOWER_TANK_ASM"))
            ?? parentIssues.FirstOrDefault(i => i.Issue?.StockIn?.Part?.Name?.Contains("Lower", StringComparison.OrdinalIgnoreCase) == true)?.Issue?.Number
            ?? (parentIssues.Count > 2 ? parentIssues[2].Issue?.Number : null)
            ?? string.Empty;

        bool oRingSet = GetDetailBool(FindDetail(parentDetails, "CLINCHING_SHORT_SIDE", "O_RING_SET_RESULT", "O_RING_SET", "ORING_SET_RESULT")) ?? true;
        string ngBoxShort = GetDetailText(FindDetail(parentDetails, "CLINCHING_SHORT_SIDE", "NG_BOX_SENSOR_SHORT_SIDE", "NG_BOX_SHORT_SIDE", "NG_BOX")) ?? "ON";

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
        string ngBoxLong = GetDetailText(FindDetail(parentDetails, "CLINCHING_LONG_SIDE", "NG_BOX_SENSOR_LONG_SIDE", "NG_BOX_LONG_SIDE", "NG_BOX")) ?? "ON";

        // 3. M-Fan Assy Lots & Details
        var childIssues = childSn?.Issues?
            .Where(x => x.Issue != null)
            .OrderBy(x => x.CreatedAt)
            .ToList() ?? new List<SerialNumberIssue>();

        string? lotFan = GetDetailText(FindDetail(childDetails, "M_FAN_ASSY", "LOT_FAN_ASM_RESULT", "LOT_FAN_ASM", "FAN_ASM"))
            ?? childIssues.FirstOrDefault(i => i.Issue?.StockIn?.Part?.Name?.Contains("Fan", StringComparison.OrdinalIgnoreCase) == true)?.Issue?.Number
            ?? (childIssues.Count > 0 ? childIssues[0].Issue?.Number : null);

        string? lotMotor = GetDetailText(FindDetail(childDetails, "M_FAN_ASSY", "LOT_MOTOR_ASM_RESULT", "LOT_MOTOR_ASM", "MOTOR_ASM"))
            ?? childIssues.FirstOrDefault(i => i.Issue?.StockIn?.Part?.Name?.Contains("Motor", StringComparison.OrdinalIgnoreCase) == true)?.Issue?.Number
            ?? (childIssues.Count > 1 ? childIssues[1].Issue?.Number : null);

        string? lotGuide = GetDetailText(FindDetail(childDetails, "M_FAN_ASSY", "LOT_GUIDE_ASM_RESULT", "LOT_GUIDE_ASM", "GUIDE_ASM"))
            ?? childIssues.FirstOrDefault(i => i.Issue?.StockIn?.Part?.Name?.Contains("Guide", StringComparison.OrdinalIgnoreCase) == true)?.Issue?.Number
            ?? (childIssues.Count > 2 ? childIssues[2].Issue?.Number : null);

        string? boltTighten = GetDetailText(FindDetail(childDetails, "M_FAN_ASSY", "BOLT_TIGHTEN_VALUE", "BOLT_TIGHTEN"));
        string? boltQty = GetDetailText(FindDetail(childDetails, "M_FAN_ASSY", "BOLT_TIGHTEN_QTY_VALUE", "BOLT_TIGHTEN_QTY", "BOLT_QTY"));
        bool? nutTighten = GetDetailBool(FindDetail(childDetails, "M_FAN_ASSY", "NUT_TIGHTEN_VALUE", "NUT_TIGHTEN"));

        // 4. M-Fan Inspection
        double? rotMax = GetDetailNumber(FindDetail(childDetails, "M_FAN_INSPECTION", "M_FAN_INSPECTION_ROTATION_SPEED_MAX", "ROTATION_SPEED_MAX", "ROT_MAX"));
        double? rotMin = GetDetailNumber(FindDetail(childDetails, "M_FAN_INSPECTION", "M_FAN_INSPECTION_ROTATION_SPEED_MIN", "ROTATION_SPEED_MIN", "ROT_MIN"));
        double? ampMax = GetDetailNumber(FindDetail(childDetails, "M_FAN_INSPECTION", "M_FAN_INSPECTION_AMPERE_MAX", "AMPERE_MAX", "AMP_MAX"));
        double? ampMin = GetDetailNumber(FindDetail(childDetails, "M_FAN_INSPECTION", "M_FAN_INSPECTION_AMPERE_MIN", "AMPERE_MIN", "AMP_MIN"));
        string? windDir = GetDetailText(FindDetail(childDetails, "M_FAN_INSPECTION", "M_FAN_INSPECTION_WIND_DIRECTION", "WIND_DIRECTION"));
        bool? mFanTest = GetDetailBool(FindDetail(childDetails, "M_FAN_INSPECTION", "M_FAN_TEST_RESULT", "MFAN_TEST_RESULT", "TEST_RESULT"));
        string? ngBoxMFan = GetDetailText(FindDetail(childDetails, "M_FAN_INSPECTION", "NG_BOX_SENSOR_M_FAN_INSPECTION", "NG_BOX_MFAN", "NG_BOX")) ?? "ON";

        // 5. ECM Assy
        bool? radCoreLabel = GetDetailBool(FindDetail(parentDetails, "ECM_ASSY", "RAD_CORE_ASM_NAME_LABEL_RESULT", "RAD_CORE_LABEL", "RAD_CORE_ASM_LABEL"));
        bool? motorFanLabel = GetDetailBool(FindDetail(parentDetails, "ECM_ASSY", "MOTOR_FAN_ASSY_LABEL_RESULT", "MOTOR_FAN_LABEL", "MOTOR_FAN_ASSY_LABEL"));
        double? ecmBolt = GetDetailNumber(FindDetail(parentDetails, "ECM_ASSY", "ECM_ASSY_BOLT_TIGHTEN_VALUE", "ECM_BOLT_TIGHTEN", "ECM_BOLT"));
        double? ecmBoltQty = GetDetailNumber(FindDetail(parentDetails, "ECM_ASSY", "ECM_ASSY_BOLT_TIGHTEN_QTY_VALUE", "ECM_BOLT_QTY", "ECM_ASSY_BOLT_QTY"));
        string? ngBoxEcm = GetDetailText(FindDetail(parentDetails, "ECM_ASSY", "NG_BOX_SENSOR_ECM_ASSY", "NG_BOX_ECM", "NG_BOX")) ?? "ON";

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
        string? ngBoxFinal = GetDetailText(FindDetail(parentDetails, "FINAL_INSPECTION", "NG_BOX_SENSOR_FINAL_INSPECTION", "NG_BOX_FINAL", "NG_BOX")) ?? "ON";

        // Overall status
        string overallStatus = log.Status ? "PASSED" : "REJECTED";

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

    private static ProcessLogListDto MapToListDto(ProcessLog log)
    {
        var sn = log.SerialNumber;
        if (sn == null) return new ProcessLogListDto();

        bool isClinching = !string.IsNullOrWhiteSpace(sn.Type)
            ? string.Equals(sn.Type, "clinching", StringComparison.OrdinalIgnoreCase)
            : sn.SerialNumberCode.StartsWith("CC", StringComparison.OrdinalIgnoreCase);

        string type = isClinching ? "clinching" : "mfan";
        object detail = isClinching ? (object)MapClinchingDetail(log) : (object)MapMFanDetail(log);

        return new ProcessLogListDto
        {
            Id               = log.Id,
            Type             = type,
            SerialNumberCode = sn.SerialNumberCode,
            Status           = log.Status,
            IsFinished       = log.IsFinished,
            CreatedAt        = log.CreatedAt,
            UpdatedAt        = log.UpdatedAt,
            Detail           = detail
        };
    }

    private static ProcessLogDto MapToDto(ProcessLog log)
    {
        var sn = log.SerialNumber;
        if (sn == null) return new ProcessLogDto();

        bool isParent = sn.SerialNumberCode.StartsWith("CC");

        var issues = new List<IssueSummaryDto>();
        issues.AddRange(MapIssues(sn.Issues, isParent ? "PARENT" : "CHILD"));

        var allDetails = new List<ProcessLogDetail>();
        if (log.Details != null)
            allDetails.AddRange(log.Details);

        if (isParent && sn.ParentRelations != null)
        {
            var childSns = sn.ParentRelations.Select(r => r.ChildSerialNumber).Where(c => c != null);
            foreach (var childSn in childSns)
            {
                issues.AddRange(MapIssues(childSn!.Issues, "CHILD"));
                if (childSn.ProcessLogs != null)
                    allDetails.AddRange(childSn.ProcessLogs.SelectMany(pl => pl.Details ?? new List<ProcessLogDetail>()));
            }
        }

        return new ProcessLogDto
        {
            Id               = log.Id,
            IsActive         = log.IsActive,
            Status           = log.Status,
            IsParent         = isParent,
            SerialNumberCode = sn.SerialNumberCode,
            Type             = sn.Type,
            CreatedAt        = log.CreatedAt,
            UpdatedAt        = log.UpdatedAt,
            Issues           = issues,
            Processes        = MapDetails(allDetails)
        };
    }

    private static List<IssueSummaryDto> MapIssues(IEnumerable<SerialNumberIssue>? issues, string issueType)
        => issues?
            .Where(sni => sni.Issue != null)
            .Select(sni => new IssueSummaryDto
            {
                IssueType   = issueType,
                IssueNumber = sni.Issue!.Number,
                PartNumber  = sni.Issue.StockIn?.Part?.Number ?? string.Empty,
                PartName    = sni.Issue.StockIn?.Part?.Name ?? string.Empty
            })
            .ToList() ?? new List<IssueSummaryDto>();

    private static List<ProcessGroupDto> MapDetails(ICollection<ProcessLogDetail>? details)
    {
        if (details == null) return new List<ProcessGroupDto>();

        return details
            .GroupBy(d => new { Id = d.Process?.Id ?? 0, Code = d.Process?.Code, Name = d.Process?.Name })
            .OrderBy(g => g.Key.Id)
            .Select(g => new ProcessGroupDto
            {
                ProcessCode = g.Key.Code ?? "UNKNOWN",
                ProcessName = g.Key.Name ?? "Unknown Process",
                Result      = !g.Any(d => d.Parameter?.DataType == "boolean" && d.ValueBoolean == false),
                Parameters  = g.Select(d => new ProcessParameterValueDto
                {
                    ParameterCode = d.Parameter?.Code,
                    ParameterName = d.Parameter?.Name,
                    Value = d.Parameter?.DataType switch
                    {
                        "boolean" => d.ValueBoolean,
                        "number"  => d.ValueNumber,
                        _         => d.ValueText
                    },
                    Status = d.Status
                }).ToList()
            }).ToList();
    }

    public async Task<ProcessLogDto> CreateProcessLogByClinchingAsync(
        CreateProcessLogRequestDto request,
        CancellationToken cancellationToken = default)
    {
        // 1. Ekstrak issue numbers dari request.Data
        var issueNumbers = ExtractIssueNumbers(request.Data);

        // 2. Generate Clinching Serial Number
        var generateRequest = new GenerateSerialNumberRequestDto
        {
            Type = "CLINCHING",
            Qty = 1,
            CreatedBy = request.OperatorUsername ?? "MQTT_CLINCHING",
            IssueNumbers = issueNumbers.Count > 0 ? issueNumbers : null
        };

        var generatedSns = (await _serialNumberService.CreateByClinchingAsync(generateRequest, cancellationToken)).ToList();
        if (generatedSns.Count == 0)
            throw new AppException("Gagal membuat serial number Clinching.", 500);

        var clinchingSerialNumberCode = generatedSns[0].SerialNumberCode;

        // 4. Set serial number ke request dan panggil CreateProcessLogWithDetailsAsync
        request.SerialNumber = clinchingSerialNumberCode;
        if (string.IsNullOrWhiteSpace(request.ProcessCode))
        {
            request.ProcessCode = "CLINCHING_SHORT_SIDE";
        }

        // 5. Mapping IsOk ke parameter codes yang sesuai dengan value true, dikarenakan process log pertama dan slalu true, karena dari PLC sudah di control
        request.Data ??= new Dictionary<string, object>();
        if (request.IsOk.HasValue && issueNumbers.Count >= 3)
        {
            request.Data["CORE_ASM_RESULT"]       = issueNumbers[0];
            request.Data["UPPER_TANK_ASM_RESULT"]  = issueNumbers[1];
            request.Data["LOWER_TANK_ASM_RESULT"]  = issueNumbers[2];
        }

        var result = await CreateProcessLogWithDetailsAsync(request, cancellationToken);

        // Print barcode label clinching short side
        await _printService.PrintClinchingShortSideAsync(clinchingSerialNumberCode, issueNumbers, cancellationToken);

        return result;
    }

    private static List<string> ExtractIssueNumbers(Dictionary<string, object>? data)
    {
        var issueNumbers = new List<string>();
        if (data == null) return issueNumbers;

        var dataInsensitive = new Dictionary<string, object>(data, StringComparer.OrdinalIgnoreCase);

        if (dataInsensitive.TryGetValue("issue_numbers", out var rawIssueNumbers))
        {
            if (rawIssueNumbers is System.Text.Json.JsonElement element && element.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    var str = item.GetString();
                    if (!string.IsNullOrWhiteSpace(str))
                        issueNumbers.Add(str);
                }
            }
            else if (rawIssueNumbers is IEnumerable<object> list)
            {
                foreach (var item in list)
                {
                    var str = item?.ToString();
                    if (!string.IsNullOrWhiteSpace(str))
                        issueNumbers.Add(str);
                }
            }
            else if (rawIssueNumbers is IEnumerable<string> strList)
            {
                foreach (var str in strList)
                {
                    if (!string.IsNullOrWhiteSpace(str))
                        issueNumbers.Add(str);
                }
            }
            else if (rawIssueNumbers is System.Text.Json.JsonElement strElement && strElement.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                var str = strElement.GetString();
                if (!string.IsNullOrWhiteSpace(str))
                    issueNumbers.Add(str);
            }
        }
        else if (dataInsensitive.TryGetValue("issue_number", out var rawIssueNumber))
        {
            if (rawIssueNumber is System.Text.Json.JsonElement strElement && strElement.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                var str = strElement.GetString();
                if (!string.IsNullOrWhiteSpace(str))
                    issueNumbers.Add(str);
            }
        }

        return issueNumbers;
    }

    public async Task<ProcessLogDto> CreateProcessLogWithDetailsAsync(
        CreateProcessLogRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.SerialNumber))
            throw new AppException("Serial number is required.", 400);

        if (string.IsNullOrWhiteSpace(request.ProcessCode))
            throw new AppException("Process code is required.", 400);

        var parameterCodes = request.Data?.Keys.ToList() ?? new List<string>();
        var existingParams = await _parameterRepository.FindAsync(
            p => parameterCodes.Contains(p.Code), cancellationToken);

        var paramValues = new List<(string parameterCode, decimal? valueNumber, string? valueText, bool? valueBoolean, bool status)>();

        if (request.Data != null)
        {
            foreach (var kvp in request.Data)
            {
                var param = existingParams.FirstOrDefault(p => p.Code == kvp.Key);
                if (param == null) continue;

                decimal? valNum = null;
                string? valText = null;
                bool? valBool = null;

                if (param.DataType == "boolean")
                {
                    valBool = ParseBoolean(kvp.Value);
                }
                else if (param.DataType == "number")
                {
                    valNum = ParseDecimal(kvp.Value);
                }
                else
                {
                    valText = ParseText(kvp.Value);
                }

                bool status = request.IsOk ?? true;
                paramValues.Add((kvp.Key, valNum, valText, valBool, status));
            }
        }

        // Tentukan apakah status proses adalah OK atau NG
        bool isOk = true;
        if (paramValues.Any(p => p.valueBoolean == false))
        {
            isOk = false;
        }

        // Simpan log proses dan detailnya ke database menggunakan repo
        var processLog = await _processLogRepository.AddProcessLogPerProcessAsync(
            request.SerialNumber,
            request.ProcessCode,
            isOk,
            paramValues,
            cancellationToken);

        // Ambil log yang baru dibuat beserta relasi detailnya untuk dikembalikan sebagai DTO
        return await GetProcessLogByIdAsync(processLog.Id, cancellationToken);
    }

    public async Task<ProcessLogDto> CreateProcessLogDetailOnlyAsync(
        CreateProcessLogRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.SerialNumber))
            throw new AppException("Serial number is required.", 400);

        if (string.IsNullOrWhiteSpace(request.ProcessCode))
            throw new AppException("Process code is required.", 400);

        // 1. Get Serial Number by Code
        var serialNumber = await _serialNumberRepository.FirstOrDefaultAsync(
            x => x.SerialNumberCode == request.SerialNumber, cancellationToken);
        if (serialNumber == null)
            throw new AppException($"Serial number '{request.SerialNumber}' not found.", 404);

        // 2. Get Process by Code
        var process = await _processRepository.FirstOrDefaultAsync(
            x => x.Code == request.ProcessCode, cancellationToken);
        if (process == null)
            throw new AppException($"Process with code '{request.ProcessCode}' not found.", 404);

        // 3. Get Process Log (Active)
        var processLog = await _processLogRepository.FirstOrDefaultAsync(
            x => x.SerialNumberId == serialNumber.Id && x.IsActive, cancellationToken);

        if (processLog == null)
        {
            // If not found, create new Process Log first
            processLog = new ProcessLog
            {
                SerialNumberId = serialNumber.Id,
                IsActive = true,
                Status = request.IsOk ?? true,
                IsFinished = request.IsOk == false ? true : request.IsFInihed,
                CreatedAt = DateTime.Now
            };
            await _processLogRepository.AddAsync(processLog, cancellationToken);
            await _processLogRepository.SaveChangesAsync(cancellationToken);
        }
        else
        {
            processLog.UpdatedAt = DateTime.Now;
            processLog.IsFinished = request.IsOk == false ? true : request.IsFInihed;
            if (request.IsOk == false)
            {
                processLog.Status = false;
            }
            _processLogRepository.Update(processLog);
            await _processLogRepository.SaveChangesAsync(cancellationToken);
        }

        // 4. Get Parameters by Codes from request.Data keys
        var parameterCodes = request.Data?.Keys.ToList() ?? new List<string>();
        var existingParams = await _parameterRepository.FindAsync(
            p => parameterCodes.Contains(p.Code), cancellationToken);

        var paramValues = new List<(string parameterCode, decimal? valueNumber, string? valueText, bool? valueBoolean, bool status)>();

        if (request.Data != null)
        {
            foreach (var kvp in request.Data)
            {
                var param = existingParams.FirstOrDefault(p => p.Code == kvp.Key);
                if (param == null) continue;

                decimal? valNum = null;
                string? valText = null;
                bool? valBool = null;

                if (param.DataType == "boolean")
                {
                    valBool = ParseBoolean(kvp.Value);
                }
                else if (param.DataType == "number")
                {
                    valNum = ParseDecimal(kvp.Value);
                }
                else
                {
                    valText = ParseText(kvp.Value);
                }

                bool status = request.IsOk ?? true;
                paramValues.Add((kvp.Key, valNum, valText, valBool, status));
            }
        }

        // 5. Insert details to the ProcessLog entity's Details collection directly
        foreach (var param in paramValues)
        {
            var parameter = existingParams.FirstOrDefault(x => x.Code == param.parameterCode);
            if (parameter == null) continue;

            var detail = new ProcessLogDetail
            {
                ProcessLogId = processLog.Id,
                ProcessId = process.Id,
                ParameterId = parameter.Id,
                ValueNumber = param.valueNumber,
                ValueText = param.valueText,
                ValueBoolean = param.valueBoolean,
                Status = param.status,
                CreatedAt = DateTime.Now
            };
            processLog.Details.Add(detail);
        }

        _processLogRepository.Update(processLog);
        await _processLogRepository.SaveChangesAsync(cancellationToken);

        return await GetProcessLogByIdAsync(processLog.Id, cancellationToken);
    }

    public async Task<ProcessLogDto> CreateProcessLogMFanAssyAsync(
        CreateProcessLogRequestDto request,
        String type = "create_with_issue_number",
        CancellationToken cancellationToken = default)
    {
        if(type == "create_with_issue_number")
        {
            return await CreateProcessLogMFanAssyWithIssueNumberAsync(request, cancellationToken);
        }
        else
        {
            return await CreateProcessLogMFanAssyWithOutIssueNumberAsync(request, cancellationToken);
        }
    }

    private async Task<ProcessLogDto> CreateProcessLogMFanAssyWithIssueNumberAsync(
        CreateProcessLogRequestDto request,
        CancellationToken cancellationToken = default
    )
    {
        // 1. Ekstrak issue numbers dari request.Data
        var issueNumbers = ExtractIssueNumbers(request.Data);
        if (issueNumbers.Count == 0 && request.Data != null)
        {
            var dataInsensitive = new Dictionary<string, object>(request.Data, StringComparer.OrdinalIgnoreCase);
            var issueKeys = new[] { "fan_asm_issue_no", "fan_motor_asm_issue_no", "fan_guide_asm_issue_no" };
            foreach (var key in issueKeys)
            {
                if (dataInsensitive.TryGetValue(key, out var rawVal))
                {
                    var issueNo = ParseText(rawVal);
                    if (!string.IsNullOrWhiteSpace(issueNo))
                        issueNumbers.Add(issueNo);
                }
            }
        }

        // 2. Generate MF serial number (Qty=1, semua issue dikaitkan, qty issue dikurangi 1)
        var generateRequest = new GenerateSerialNumberRequestDto
        {
            Type         = "MFANASSY",
            Qty          = 1,
            CreatedBy    = request.OperatorUsername ?? "MQTT_M_FAN_ASSY",
            IssueNumbers = issueNumbers.Count > 0 ? issueNumbers : null,
            SkipConsume  = false
        };

        var generatedSns = (await _serialNumberService.CreateByMFanAsync(generateRequest, cancellationToken)).ToList();
        if (generatedSns.Count == 0)
            throw new AppException("Gagal membuat serial number MFan.", 500);

        var mfSerialNumberCode = generatedSns[0].SerialNumberCode;

        // 3. Publish MQTT ke topic data/process/m-fan-assy/process-scan
        var fanAsmIssueNo      = issueNumbers.Count > 0 ? issueNumbers[0] : null;
        var fanMotorAsmIssueNo = issueNumbers.Count > 1 ? issueNumbers[1] : null;
        var fanGuideAsmIssueNo = issueNumbers.Count > 2 ? issueNumbers[2] : null;

        var fanAsmQtyRemaining      = fanAsmIssueNo      != null ? await _issueRepository.GetFinalStockByIssueNumberAsync(fanAsmIssueNo,      cancellationToken) : null;
        var fanMotorAsmQtyRemaining = fanMotorAsmIssueNo != null ? await _issueRepository.GetFinalStockByIssueNumberAsync(fanMotorAsmIssueNo, cancellationToken) : null;
        var fanGuideAsmQtyRemaining = fanGuideAsmIssueNo != null ? await _issueRepository.GetFinalStockByIssueNumberAsync(fanGuideAsmIssueNo, cancellationToken) : null;

        var mqttPayload = new
        {
            serial_number     = mfSerialNumberCode,
            operator_username = request.OperatorUsername ?? "MQTT_M_FAN_ASSY",
            timestamp         = DateTime.UtcNow,
            data = new
            {
                fan_asm_issue_no            = fanAsmIssueNo,
                fan_asm_qty_remaining       = fanAsmQtyRemaining,
                fan_motor_asm_issue_no      = fanMotorAsmIssueNo,
                fan_motor_asm_qty_remaining = fanMotorAsmQtyRemaining,
                fan_guide_asm_issue_no      = fanGuideAsmIssueNo,
                fan_guide_asm_qty_remaining = fanGuideAsmQtyRemaining
            }
        };

        await _mqttPublisher.PublishAsync("data/process/m-fan-assy/process-scan", mqttPayload, cancellationToken);

        // 4. Pastikan detail disimpan ke MF serial number
        request.Data ??= new Dictionary<string, object>();
        request.ProcessCode = "M_FAN_ASSY";
        if (request.IsOk.HasValue && issueNumbers.Count >= 3)
        {
            request.Data["FAN_ASM_RESULT"] = issueNumbers[0];
            request.Data["MOTOR_ASM_RESULT"] = issueNumbers[1];
            request.Data["FUN_GUIDE_ASM_RESULT"] = issueNumbers[2];
        }
        
        request.SerialNumber = mfSerialNumberCode;
        
        var result = await CreateProcessLogDetailOnlyAsync(request, cancellationToken);

        // 5. Print barcode / label M-Fan Assy
        await _printService.PrintMFanAssyAsync(mfSerialNumberCode, issueNumbers, cancellationToken);

        return result;
    }

    public async Task<ProcessLogDto> CreateProcessLogEcmAssyAsync(
        CreateProcessLogRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var clinchingSnCode = !string.IsNullOrWhiteSpace(request.SerialNumberClinching)
            ? request.SerialNumberClinching
            : request.SerialNumber;

        var mfanSnCode = !string.IsNullOrWhiteSpace(request.SerialNumberMFanAssy)
            ? request.SerialNumberMFanAssy
            : null;

        if (string.IsNullOrWhiteSpace(clinchingSnCode))
            throw new AppException("Serial number clinching (CC) is required.", 400);

        if (string.IsNullOrWhiteSpace(mfanSnCode))
            throw new AppException("Serial number M-Fan (MF) is required.", 400);

        // 1. Ambil parent (CC) dan child (MF) dari DB
        var parentSerialNumber = await _serialNumberRepository.FirstOrDefaultAsync(
            x => x.SerialNumberCode == clinchingSnCode, cancellationToken);
        if (parentSerialNumber == null)
            throw new AppException($"Clinching serial number '{clinchingSnCode}' tidak ditemukan.", 404);

        var childSerialNumber = await _serialNumberRepository.FirstOrDefaultAsync(
            x => x.SerialNumberCode == mfanSnCode, cancellationToken);
        if (childSerialNumber == null)
            throw new AppException($"M-Fan serial number '{mfanSnCode}' tidak ditemukan.", 404);

        // 2. Hubungkan relasi SerialNumberRelation jika belum ada
        var existingRelation = parentSerialNumber.ParentRelations
            .FirstOrDefault(r => r.ChildSerialNumberId == childSerialNumber.Id);

        if (existingRelation == null)
        {
            var relation = new SerialNumberRelation
            {
                ParentSerialNumberId = parentSerialNumber.Id,
                ChildSerialNumberId  = childSerialNumber.Id,
                CreatedAt            = DateTime.UtcNow,
                CreatedBy            = request.OperatorUsername ?? "MQTT_ECM_ASSY"
            };
            parentSerialNumber.ParentRelations.Add(relation);
            _serialNumberRepository.Update(parentSerialNumber);
            await _serialNumberRepository.SaveChangesAsync(cancellationToken);
        }

        // 3. Simpan process log detail untuk ECM_ASSY pada serial number CC
        request.SerialNumber = clinchingSnCode;
        request.ProcessCode = "ECM_ASSY";
        return await CreateProcessLogDetailOnlyAsync(request, cancellationToken);
    }


    private async Task<ProcessLogDto> CreateProcessLogMFanAssyWithOutIssueNumberAsync(
        CreateProcessLogRequestDto request,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(request.SerialNumber))
            throw new AppException("Serial number (MF) is required.", 400);

        // Langsung validasi child MF serial number dari DB
        var childSerialNumber = await _serialNumberRepository.FirstOrDefaultAsync(
            x => x.SerialNumberCode == request.SerialNumber, cancellationToken);
            
        if (childSerialNumber == null)
            throw new AppException($"MF serial number '{request.SerialNumber}' tidak ditemukan.", 404);

        if (request.Data != null)
        {
            var dataInsensitive = new Dictionary<string, object>(request.Data, StringComparer.OrdinalIgnoreCase);

            if (dataInsensitive.TryGetValue("BOLT_TIGHTEN_RESULT", out var boltResult))
                request.Data["BOLT_TIGHTEN_RESULT"] = boltResult;

            if (dataInsensitive.TryGetValue("BOLT_TIGHTEN_VALUE", out var boltVal))
                request.Data["BOLT_TIGHTEN_VALUE"] = boltVal;

            if (dataInsensitive.TryGetValue("NUT_TIGHTEN_RESULT", out var nutResult))
                request.Data["NUT_TIGHTEN_RESULT"] = nutResult;
        }

        return await CreateProcessLogDetailOnlyAsync(request, cancellationToken);
    }

    private static bool? ParseBoolean(object? val)
    {
        if (val == null) return null;

        if (val is System.Text.Json.JsonElement element)
        {
            if (element.ValueKind == System.Text.Json.JsonValueKind.True) return true;
            if (element.ValueKind == System.Text.Json.JsonValueKind.False) return false;

            var strVal = element.GetString()?.Trim().ToUpperInvariant();
            if (strVal == "OK" || strVal == "TRUE" || strVal == "1") return true;
            if (strVal == "NG" || strVal == "FALSE" || strVal == "0") return false;
            return null;
        }

        var str = val.ToString()?.Trim().ToUpperInvariant();
        if (str == "OK" || str == "TRUE" || str == "1") return true;
        if (str == "NG" || str == "FALSE" || str == "0") return false;

        if (val is bool b) return b;
        return null;
    }

    private static decimal? ParseDecimal(object? val)
    {
        if (val == null) return null;

        if (val is System.Text.Json.JsonElement element)
        {
            if (element.ValueKind == System.Text.Json.JsonValueKind.Number && element.TryGetDecimal(out var d))
            {
                return d;
            }
            if (decimal.TryParse(element.GetString(), out var parsedStr))
            {
                return parsedStr;
            }
            return null;
        }

        if (decimal.TryParse(val.ToString(), out var result))
        {
            return result;
        }
        return null;
    }

    private static string? ParseText(object? val)
    {
        if (val == null) return null;
        if (val is System.Text.Json.JsonElement element)
        {
            return element.ValueKind == System.Text.Json.JsonValueKind.String 
                ? element.GetString() 
                : element.GetRawText();
        }
        return val.ToString();
    }

    public async Task<bool> ValidateSerialNumberProcessLogAsync(
        string serialNumberCode,
        CancellationToken cancellationToken = default)
    {
        var serialNumber = await _serialNumberRepository.FirstOrDefaultAsync(
            s => s.SerialNumberCode == serialNumberCode, cancellationToken);

        if (serialNumber == null) return false;

        var processLog = await _processLogRepository.FirstOrDefaultAsync(
            pl => pl.SerialNumberId == serialNumber.Id, cancellationToken);

        if (processLog == null) return false;

        return processLog.IsFinished && !processLog.Status;
    }
}
