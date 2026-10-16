using System.Globalization;
using Mapster;
using TraceabilitySystem.Application.DTOs.ProcessLog;
using TraceabilitySystem.Application.DTOs.SerialNumber;
using TraceabilitySystem.Application.DTOs.TraceabilityLog;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Application.Mappers;
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
    private readonly IIssueService _issueService;
    private readonly IMqttPublisher _mqttPublisher;
    private readonly IPrintService _printService;
    private readonly IMqttPublisherStatusRepository _mqttPublisherStatusRepository;

    public ProcessLogService(
        IProcessLogRepository processLogRepository,
        IIssueRepository issueRepository,
        IStockInRepository stockInRepository,
        ISerialNumberRepository serialNumberRepository,
        IParameterRepository parameterRepository,
        IProcessRepository processRepository,
        ISerialNumberService serialNumberService,
        IIssueService issueService,
        IMqttPublisher mqttPublisher,
        IPrintService printService,
        IMqttPublisherStatusRepository mqttPublisherStatusRepository)
    {
        _processLogRepository = processLogRepository;
        _issueRepository = issueRepository;
        _stockInRepository = stockInRepository;
        _serialNumberRepository = serialNumberRepository;
        _parameterRepository = parameterRepository;
        _processRepository = processRepository;
        _serialNumberService = serialNumberService;
        _issueService = issueService;
        _mqttPublisher = mqttPublisher;
        _printService = printService;
        _mqttPublisherStatusRepository = mqttPublisherStatusRepository;
    }

    public async Task<PagedResult<ProcessLogListDto>> GetProcessLogsAsync(
        int page,
        int pageSize,
        string? serialNumberCode = null,
        bool? status = null,
        bool? isFinished = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var (logs, totalCount) = await _processLogRepository.GetPagedLogsAsync(
            page, pageSize, serialNumberCode, status, isFinished, startDate, endDate, clinchingOnly: false, cancellationToken: cancellationToken);

        var dtos = logs.Select(MapToListDto).ToList();

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
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var (logs, totalCount) = await _processLogRepository.GetPagedLogsAsync(
            page, pageSize, serialNumberCode, status, isFinished, startDate, endDate, clinchingOnly: true, cancellationToken: cancellationToken);

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

        var allProcesses = await _processRepository.GetAllWithParametersAsync(cancellationToken);

        var snIds = new List<int>();
        var snCodes = new List<string>();
        if (log.SerialNumber != null)
        {
            snIds.Add(log.SerialNumber.Id);
            snCodes.Add(log.SerialNumber.SerialNumberCode);
            if (log.SerialNumber.ParentRelations != null)
            {
                snIds.AddRange(log.SerialNumber.ParentRelations
                    .Where(r => r.ChildSerialNumber != null)
                    .Select(r => r.ChildSerialNumber!.Id));
                snCodes.AddRange(log.SerialNumber.ParentRelations
                    .Where(r => r.ChildSerialNumber != null)
                    .Select(r => r.ChildSerialNumber!.SerialNumberCode));
            }
        }

        var histories = snIds.Count > 0
            ? await _processLogRepository.GetHistoriesBySerialNumberIdsAsync(snIds, cancellationToken)
            : new List<ProcessHistory>();

        var disabledMqttStatuses = snCodes.Count > 0
            ? await _mqttPublisherStatusRepository.GetByReferenceCodesAsync(
                snCodes,
                MqttPublisherReferenceTypes.ProcessLog,
                cancellationToken)
            : new List<MqttPublisherStatus>();

        return MapToDto(log, allProcesses, histories, disabledMqttStatuses);
    }

    private static readonly HashSet<string> OverallProcessCodes =
[
    "ECM_ASSY",
    "FINAL_INSPECTION"
];

    public async Task<ProcessLogDto> GetProcessLogBySerialNumberAsync(string serialNumber, CancellationToken cancellationToken = default)
    {
        var log = await _processLogRepository.GetLogBySerialNumberAsync(serialNumber, cancellationToken);
        if (log == null) throw new NotFoundException(nameof(ProcessLog), serialNumber);

        var allProcesses = await _processRepository.GetAllWithParametersAsync(cancellationToken);

        var snIds = new List<int>();
        var snCodes = new List<string> { serialNumber };
        if (log.SerialNumber != null)
        {
            snIds.Add(log.SerialNumber.Id);
            if (log.SerialNumber.ParentRelations != null)
            {
                snIds.AddRange(log.SerialNumber.ParentRelations
                    .Where(r => r.ChildSerialNumber != null)
                    .Select(r => r.ChildSerialNumber!.Id));
                snCodes.AddRange(log.SerialNumber.ParentRelations
                    .Where(r => r.ChildSerialNumber != null)
                    .Select(r => r.ChildSerialNumber!.SerialNumberCode));
            }
        }

        var histories = snIds.Count > 0
            ? await _processLogRepository.GetHistoriesBySerialNumberIdsAsync(snIds, cancellationToken)
            : new List<ProcessHistory>();

        var disabledMqttStatuses = await _mqttPublisherStatusRepository.GetByReferenceCodesAsync(
            snCodes,
            MqttPublisherReferenceTypes.ProcessLog,
            cancellationToken);

        return MapToDto(log, allProcesses, histories, disabledMqttStatuses);
    }

    // ── Private helpers ─────────────────────────────────────────────────────────

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
            if (detail.ValueNumber.Value == 0) return false;
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

    public static ProcessLogClinchingDetailDto MapClinchingDetail(ProcessLog log)
    {
        var sn = log.SerialNumber;
        var clinchingSn = sn?.SerialNumberCode ?? string.Empty;
        var parentDetails = log.Details ?? new List<ProcessLogDetail>();

        var parentIssues = sn?.Issues?
            .Where(x => x.Issue != null)
            .OrderBy(x => x.CreatedAt)
            .ToList() ?? new List<SerialNumberIssue>();

        string coreAsm = parentIssues.FirstOrDefault(i => i.Issue?.StockIn?.Part?.Name?.Contains("Core", StringComparison.OrdinalIgnoreCase) == true)?.Issue?.Number
            ?? GetIssueNumberFromDetail(FindDetail(parentDetails, "CLINCHING_SHORT_SIDE", "CORE_ASM_RESULT", "CORE_ASM", "CORE_ASM_ISSUE_NO"))
            ?? (parentIssues.Count > 0 ? parentIssues[0].Issue?.Number : null)
            ?? string.Empty;

        string upperTank = parentIssues.FirstOrDefault(i => i.Issue?.StockIn?.Part?.Name?.Contains("Upper", StringComparison.OrdinalIgnoreCase) == true)?.Issue?.Number
            ?? GetIssueNumberFromDetail(FindDetail(parentDetails, "CLINCHING_SHORT_SIDE", "UPPER_TANK_ASM_RESULT", "UPPER_TANK_ASM", "UPPER_TANK_ASM_ISSUE_NO"))
            ?? (parentIssues.Count > 1 ? parentIssues[1].Issue?.Number : null)
            ?? string.Empty;

        string lowerTank = parentIssues.FirstOrDefault(i => i.Issue?.StockIn?.Part?.Name?.Contains("Lower", StringComparison.OrdinalIgnoreCase) == true)?.Issue?.Number
            ?? GetIssueNumberFromDetail(FindDetail(parentDetails, "CLINCHING_SHORT_SIDE", "LOWER_TANK_ASM_RESULT", "LOWER_TANK_ASM", "LOWER_TANK_ASM_ISSUE_NO"))
            ?? (parentIssues.Count > 2 ? parentIssues[2].Issue?.Number : null)
            ?? string.Empty;

        int? oRingSet = GetDetailInt(FindDetail(parentDetails, "CLINCHING_SHORT_SIDE", "O_RING_SET_RESULT", "O_RING_SET", "ORING_SET_RESULT"));
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
        string ngBoxLong = GetDetailText(FindDetail(parentDetails, "CLINCHING_LONG_SIDE", "NG_BOX_SENSOR_LONG_SIDE", "NG_BOX_LONG_SIDE", "NG_BOX")) ?? "ON";

        // 2.5. HE Leak
        int? capTypePos = GetDetailInt(FindDetail(parentDetails, "HE_LEAK", "CAP_TYPE_POSITION_RESULT", "CAP_TYPE_POSITION", "CAP_TYPE"));
        int? leakResult = GetDetailInt(FindDetail(parentDetails, "HE_LEAK", "LEAK_RESULT", "LEAK_TEST_RESULT", "LEAK_STATUS"));
        double? leakValue = GetDetailNumber(FindDetail(parentDetails, "HE_LEAK", "LEAK_LAST_LEAKAGE_VALUE", "LEAK_LAST_LEAKAGE", "LEAK_VALUE", "LEAKAGE_VALUE"));

        return new ProcessLogClinchingDetailDto
        {
            SerialNumberClinching = clinchingSn,
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
            LeakLastLeakageValue = leakValue
        };
    }

    public static ProcessLogMFanDetailDto MapMFanDetail(ProcessLog log)
    {
        var sn = log.SerialNumber;
        var mFanSn = sn?.SerialNumberCode ?? string.Empty;
        var details = log.Details ?? new List<ProcessLogDetail>();

        var issues = sn?.Issues?
            .Where(x => x.Issue != null)
            .OrderBy(x => x.CreatedAt)
            .ToList() ?? new List<SerialNumberIssue>();

        string? lotFan = issues.FirstOrDefault(i => i.Issue?.StockIn?.Part?.Name?.Contains("Fan", StringComparison.OrdinalIgnoreCase) == true &&
                                                   i.Issue?.StockIn?.Part?.Name?.Contains("Motor", StringComparison.OrdinalIgnoreCase) != true &&
                                                   i.Issue?.StockIn?.Part?.Name?.Contains("Guide", StringComparison.OrdinalIgnoreCase) != true)?.Issue?.Number
            ?? GetIssueNumberFromDetail(FindDetail(details, "M_FAN_ASSY", "LOT_FAN_ASM_RESULT", "LOT_FAN_ASM", "FAN_ASM", "FAN_ASM_RESULT", "FAN_ASM_ISSUE_NO"))
            ?? (issues.Count > 0 ? issues[0].Issue?.Number : null);

        string? lotMotor = issues.FirstOrDefault(i => i.Issue?.StockIn?.Part?.Name?.Contains("Motor", StringComparison.OrdinalIgnoreCase) == true)?.Issue?.Number
            ?? GetIssueNumberFromDetail(FindDetail(details, "M_FAN_ASSY", "LOT_MOTOR_ASM_RESULT", "LOT_MOTOR_ASM", "MOTOR_ASM", "MOTOR_ASM_RESULT", "FAN_MOTOR_ASM_ISSUE_NO", "MOTOR_ASM_ISSUE_NO"))
            ?? (issues.Count > 1 ? issues[1].Issue?.Number : null);

        string? lotGuide = issues.FirstOrDefault(i => i.Issue?.StockIn?.Part?.Name?.Contains("Guide", StringComparison.OrdinalIgnoreCase) == true)?.Issue?.Number
            ?? GetIssueNumberFromDetail(FindDetail(details, "M_FAN_ASSY", "LOT_GUIDE_ASM_RESULT", "LOT_GUIDE_ASM", "GUIDE_ASM", "FUN_GUIDE_ASM_RESULT", "FAN_GUIDE_ASM_RESULT", "FAN_GUIDE_ASM_ISSUE_NO", "GUIDE_ASM_ISSUE_NO"))
            ?? (issues.Count > 2 ? issues[2].Issue?.Number : null);

        string? boltTighten = GetDetailText(FindDetail(details, "M_FAN_ASSY", "BOLT_TIGHTEN_VALUE", "BOLT_TIGHTEN"));
        string? boltQty = GetDetailText(FindDetail(details, "M_FAN_ASSY", "BOLT_TIGHTEN_QTY_VALUE", "BOLT_TIGHTEN_QTY", "BOLT_QTY"));
        string? nutTighten = GetDetailText(FindDetail(details, "M_FAN_ASSY", "NUT_TIGHTEN_VALUE", "NUT_TIGHTEN"));

        double? rotMax = GetDetailNumber(FindDetail(details, null, "M_FAN_INSPECTION_ROTATION_SPEED_MAX_VALUE", "M_FAN_INSPECTION_ROTATION_SPEED_MAX", "ROTATION_SPEED_MAX", "ROT_MAX"));
        double? rotMin = GetDetailNumber(FindDetail(details, null, "M_FAN_INSPECTION_ROTATION_SPEED_MIN_VALUE", "M_FAN_INSPECTION_ROTATION_SPEED_MIN", "ROTATION_SPEED_MIN", "ROT_MIN"));
        double? ampMax = GetDetailNumber(FindDetail(details, null, "M_FAN_INSPECTION_AMPERE_MAX_VALUE", "M_FAN_INSPECTION_AMPERE_MAX", "AMPERE_MAX", "AMP_MAX"));
        double? ampMin = GetDetailNumber(FindDetail(details, null, "M_FAN_INSPECTION_AMPERE_MIN_VALUE", "M_FAN_INSPECTION_AMPERE_MIN", "AMPERE_MIN", "AMP_MIN"));
        string? windDir = GetDetailText(FindDetail(details, null, "M_FAN_INSPECTION_WIND_DIRECTION_VALUE", "M_FAN_INSPECTION_WIND_DIRECTION", "WIND_DIRECTION"));
        int? mFanTest = GetDetailInt(FindDetail(details, null, "M_FAN_TEST_RESULT", "MFAN_TEST_RESULT", "TEST_RESULT"));
        string? ngBoxMFan = GetDetailText(FindDetail(details, null, "NG_BOX_SENSOR_M_FAN_INSPECTION_VALUE", "NG_BOX_SENSOR_M_FAN_INSPECTION", "NG_BOX_MFAN", "NG_BOX")) ?? "ON";

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

        string coreAsm = parentIssues.FirstOrDefault(i => i.Issue?.StockIn?.Part?.Name?.Contains("Core", StringComparison.OrdinalIgnoreCase) == true)?.Issue?.Number
            ?? GetIssueNumberFromDetail(FindDetail(parentDetails, "CLINCHING_SHORT_SIDE", "CORE_ASM_RESULT", "CORE_ASM", "CORE_ASM_ISSUE_NO"))
            ?? (parentIssues.Count > 0 ? parentIssues[0].Issue?.Number : null)
            ?? string.Empty;

        string upperTank = parentIssues.FirstOrDefault(i => i.Issue?.StockIn?.Part?.Name?.Contains("Upper", StringComparison.OrdinalIgnoreCase) == true)?.Issue?.Number
            ?? GetIssueNumberFromDetail(FindDetail(parentDetails, "CLINCHING_SHORT_SIDE", "UPPER_TANK_ASM_RESULT", "UPPER_TANK_ASM", "UPPER_TANK_ASM_ISSUE_NO"))
            ?? (parentIssues.Count > 1 ? parentIssues[1].Issue?.Number : null)
            ?? string.Empty;

        string lowerTank = parentIssues.FirstOrDefault(i => i.Issue?.StockIn?.Part?.Name?.Contains("Lower", StringComparison.OrdinalIgnoreCase) == true)?.Issue?.Number
            ?? GetIssueNumberFromDetail(FindDetail(parentDetails, "CLINCHING_SHORT_SIDE", "LOWER_TANK_ASM_RESULT", "LOWER_TANK_ASM", "LOWER_TANK_ASM_ISSUE_NO"))
            ?? (parentIssues.Count > 2 ? parentIssues[2].Issue?.Number : null)
            ?? string.Empty;

        int? oRingSet = GetDetailInt(FindDetail(parentDetails, "CLINCHING_SHORT_SIDE", "O_RING_SET_RESULT", "O_RING_SET", "ORING_SET_RESULT"));
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
        string ngBoxLong = GetDetailText(FindDetail(parentDetails, "CLINCHING_LONG_SIDE", "NG_BOX_SENSOR_LONG_SIDE", "NG_BOX_LONG_SIDE", "NG_BOX")) ?? "ON";

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

        // 4. M-Fan Inspection
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

        // Overall status: Clinching, HE, M-Fan, ECM Assy, dan Final Inspection SEMUANYA harus OK (1 / true).
        // Jika salah satu bernilai false / NG (2) / Error (0), maka OverallStatus = false.
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

    private static ProcessLogListDto MapToListDto(ProcessLog log)
    {
        var sn = log.SerialNumber;
        if (sn == null) return new ProcessLogListDto();

        bool isClinching = !string.IsNullOrWhiteSpace(sn.Type)
            ? string.Equals(sn.Type, "clinching", StringComparison.OrdinalIgnoreCase)
            : (sn.SerialNumberCode.StartsWith("PVRA", StringComparison.OrdinalIgnoreCase) || sn.SerialNumberCode.StartsWith("CC", StringComparison.OrdinalIgnoreCase));

        string type = isClinching ? "clinching" : "mfan";

        return new ProcessLogListDto
        {
            Id               = log.Id,
            Type             = type,
            SerialNumberCode = sn.SerialNumberCode,
            Status           = log.Status,
            IsFinished       = log.IsFinished,
            CreatedAt        = DateTime.SpecifyKind(log.CreatedAt, DateTimeKind.Utc),
            UpdatedAt        = log.UpdatedAt.HasValue ? DateTime.SpecifyKind(log.UpdatedAt.Value, DateTimeKind.Utc) : null
        };
    }

    private static ProcessLogDto MapToDto(
        ProcessLog log, 
        List<Process>? masterProcesses = null, 
        List<ProcessHistory>? histories = null,
        List<MqttPublisherStatus>? disabledMqttStatuses = null)
    {
        var sn = log.SerialNumber;
        if (sn == null) return new ProcessLogDto();

        bool isParent = sn.SerialNumberCode.StartsWith("PVRA", StringComparison.OrdinalIgnoreCase) || 
                        sn.SerialNumberCode.StartsWith("CC", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(sn.Type, "CLINCHING", StringComparison.OrdinalIgnoreCase);

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
            CreatedAt        = DateTime.SpecifyKind(log.CreatedAt, DateTimeKind.Utc),
            UpdatedAt        = log.UpdatedAt.HasValue ? DateTime.SpecifyKind(log.UpdatedAt.Value, DateTimeKind.Utc) : null,
            Issues           = issues,
            Processes        = MapDetails(allDetails, masterProcesses, isParent, histories, disabledMqttStatuses)
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

    private static List<ProcessGroupDto> MapDetails(
        ICollection<ProcessLogDetail>? details, 
        List<Process>? masterProcesses = null, 
        bool isParent = true, 
        List<ProcessHistory>? histories = null,
        List<MqttPublisherStatus>? disabledMqttStatuses = null)
    {
        if (masterProcesses == null || masterProcesses.Count == 0)
        {
            if (details == null) return new List<ProcessGroupDto>();

            return details
                .Where(d => d.Parameter == null || d.Parameter.ShowInDisplay)
                .GroupBy(d => new { Id = d.Process?.Id ?? 0, Code = d.Process?.Code, Name = d.Process?.Name })
                .OrderBy(g => g.Key.Id)
                .Select(g =>
                {
                    var matchedStatus = disabledMqttStatuses?.FirstOrDefault(s =>
                        string.Equals(s.ProcessCode, g.Key.Code, StringComparison.OrdinalIgnoreCase) && !s.IsPublisher);

                    bool isPublisher = matchedStatus == null;
                    string? status = !isPublisher ? (matchedStatus?.Status ?? "Disabled") : null;
                    string? reason = !isPublisher ? (matchedStatus?.Reason ?? $"Process '{g.Key.Code}' MQTT publisher is disabled by configuration.") : null;

                    return new ProcessGroupDto
                    {
                        ProcessCode = g.Key.Code ?? "UNKNOWN",
                        ProcessName = g.Key.Name ?? "Unknown Process",
                        IsMqttPublisher = isPublisher,
                        MqttPublisherStatus = status,
                        Reason = reason,
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
                        }).ToList(),
                        History = (histories ?? Enumerable.Empty<ProcessHistory>())
                            .Where(h => (g.Key.Id != 0 && h.ProcessId == g.Key.Id) || string.Equals(h.Process?.Code, g.Key.Code, StringComparison.OrdinalIgnoreCase))
                            .OrderByDescending(h => h.CreatedAt)
                            .ThenByDescending(h => h.Id)
                            .Select(h => new ProcessHistoryItemDto
                            {
                                Id            = h.Id,
                                ParameterCode = h.Parameter?.Code ?? string.Empty,
                                ParameterName = !string.IsNullOrWhiteSpace(h.Parameter?.Description) ? h.Parameter.Description : (h.Parameter?.Name ?? string.Empty),
                                Value         = h.Value,
                                Status        = DetermineHistoryStatus(h.Parameter, h.Value),
                                CreatedAt     = DateTime.SpecifyKind(h.CreatedAt, DateTimeKind.Utc)
                            }).ToList()
                    };
                }).ToList();
        }

        var detailsList = details?.ToList() ?? new List<ProcessLogDetail>();
        var result = new List<ProcessGroupDto>();

        var relevantProcesses = isParent
            ? masterProcesses.Where(p => IsClinchingProcess(p.Code) || OverallProcessCodes.Contains(p.Code))
            : masterProcesses.Where(p => IsMFanProcess(p.Code));

        foreach (var proc in relevantProcesses.Where(p => p.IsActive).OrderBy(p => p.Order ?? int.MaxValue).ThenBy(p => p.Id))
        {
            var procParams = proc.ProcessParameters?
                .Where(pp => pp.Parameter != null && pp.Parameter.IsActive && pp.Parameter.ShowInDisplay)
                .OrderBy(pp => pp.Parameter.Order ?? int.MaxValue)
                .ThenBy(pp => pp.Parameter.Id)
                .Select(pp => pp.Parameter!)
                .ToList() ?? new List<Parameter>();

            var paramDtos = new List<ProcessParameterValueDto>();

            foreach (var param in procParams)
            {
                var matched = detailsList.FirstOrDefault(d =>
                    (d.ProcessId == proc.Id || string.Equals(d.Process?.Code, proc.Code, StringComparison.OrdinalIgnoreCase)) &&
                    (d.ParameterId == param.Id || string.Equals(d.Parameter?.Code, param.Code, StringComparison.OrdinalIgnoreCase)));

                if (matched != null)
                {
                    paramDtos.Add(new ProcessParameterValueDto
                    {
                        ParameterCode = param.Code,
                        ParameterName = !string.IsNullOrWhiteSpace(param.Description) ? param.Description : param.Name,
                        Value = param.DataType switch
                        {
                            "boolean" => matched.ValueBoolean,
                            "number"  => matched.ValueNumber,
                            _         => matched.ValueText ?? matched.DisplayValue
                        },
                        Status = matched.Status
                    });
                }
                else
                {
                    paramDtos.Add(new ProcessParameterValueDto
                    {
                        ParameterCode = param.Code,
                        ParameterName = !string.IsNullOrWhiteSpace(param.Description) ? param.Description : param.Name,
                        Value = "-",
                        Status = false
                    });
                }
            }

            var procHistories = (histories ?? Enumerable.Empty<ProcessHistory>())
                .Where(h => h.ProcessId == proc.Id || string.Equals(h.Process?.Code, proc.Code, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(h => h.CreatedAt)
                .ThenByDescending(h => h.Id)
                .Select(h => new ProcessHistoryItemDto
                {
                    Id            = h.Id,
                    ParameterCode = h.Parameter?.Code ?? string.Empty,
                    ParameterName = !string.IsNullOrWhiteSpace(h.Parameter?.Description) ? h.Parameter.Description : (h.Parameter?.Name ?? string.Empty),
                    Value         = h.Value,
                    Status        = DetermineHistoryStatus(h.Parameter, h.Value),
                    CreatedAt     = DateTime.SpecifyKind(h.CreatedAt, DateTimeKind.Utc)
                })
                .ToList();

            var matchedStatus = disabledMqttStatuses?.FirstOrDefault(s =>
                string.Equals(s.ProcessCode, proc.Code, StringComparison.OrdinalIgnoreCase) && !s.IsPublisher);

            bool isPublisher = proc.IsMqttPublisher && matchedStatus == null;
            string? status = !isPublisher ? (matchedStatus?.Status ?? "Disabled") : null;
            string? reason = !isPublisher ? (matchedStatus?.Reason ?? $"Process '{proc.Code}' MQTT publisher is disabled by configuration.") : null;

            result.Add(new ProcessGroupDto
            {
                ProcessCode = proc.Code,
                ProcessName = proc.Name,
                IsMqttPublisher = isPublisher,
                MqttPublisherStatus = status,
                Reason = reason,
                Result = paramDtos.All(p => p.Value is string s && s == "-" ? true : p.Status),
                Parameters = paramDtos,
                History = procHistories
            });
        }

        return result;
    }

    public static bool IsClinchingProcess(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        return string.Equals(code, "CLINCHING_SHORT_SIDE", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(code, "CLINCHING_LONG_SIDE", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(code, "HE_LEAK", StringComparison.OrdinalIgnoreCase) ||
               code.Contains("CLINCHING", StringComparison.OrdinalIgnoreCase) ||
               code.Contains("LEAK", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsMFanProcess(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        return string.Equals(code, "M_FAN_ASSY", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(code, "M_FAN_INSPECTION", StringComparison.OrdinalIgnoreCase) ||
               code.Contains("FAN", StringComparison.OrdinalIgnoreCase);
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

        // 4. Set serial number ke request dan panggil CreateProcessLogDetailOnlyAsync
        request.SerialNumber = clinchingSerialNumberCode;
        if (string.IsNullOrWhiteSpace(request.ProcessCode))
        {
            request.ProcessCode = "CLINCHING_SHORT_SIDE";
        }

        // 5. Mapping Issue Numbers ke parameter codes via IssueService
        request.Data ??= new Dictionary<string, object>();
        var mappedParams = new Dictionary<string, object>();
        if (issueNumbers.Count > 0)
        {
            mappedParams = await _issueService.MapClinchingIssuesToParametersAsync(issueNumbers, cancellationToken);
            foreach (var (key, value) in mappedParams)
            {
                request.Data[key] = value;
            }
        }

        var result = await CreateProcessLogDetailOnlyAsync(request, recordHistory: false, cancellationToken);

        // 6. Print barcode label clinching short side
        // isPrinted: 0 = masih process/printing, 1 = berhasil, 2 = gagal
        int isPrinted = 0;
        try
        {
            await _printService.PrintClinchingShortSideAsync(clinchingSerialNumberCode, issueNumbers, cancellationToken);
            isPrinted = 1;
        }
        catch
        {
            isPrinted = 2;
        }

        // 7. Publish MQTT ke topic data/process/clinching-short-side/process-scan
        var coreIssueNo      = mappedParams.TryGetValue("CORE_ASM_VALUE", out var c) ? c?.ToString() : (issueNumbers.Count > 0 ? issueNumbers[0] : null);
        var upperTankIssueNo = mappedParams.TryGetValue("UPPER_TANK_ASM_VALUE", out var u) ? u?.ToString() : (issueNumbers.Count > 1 ? issueNumbers[1] : null);
        var lowerTankIssueNo = mappedParams.TryGetValue("LOWER_TANK_ASM_VALUE", out var l) ? l?.ToString() : (issueNumbers.Count > 2 ? issueNumbers[2] : null);

        var coreQtyRemaining      = coreIssueNo      != null ? await _issueRepository.GetFinalStockByIssueNumberAsync(coreIssueNo, cancellationToken) : null;
        var upperTankQtyRemaining = upperTankIssueNo != null ? await _issueRepository.GetFinalStockByIssueNumberAsync(upperTankIssueNo, cancellationToken) : null;
        var lowerTankQtyRemaining = lowerTankIssueNo != null ? await _issueRepository.GetFinalStockByIssueNumberAsync(lowerTankIssueNo, cancellationToken) : null;

        var mqttPayload = new
        {
            serial_number     = clinchingSerialNumberCode,
            operator_username = request.OperatorUsername ?? "MQTT_CLINCHING",
            is_printed        = isPrinted,
            timestamp         = DateTime.UtcNow,
            data = new
            {
                core_asm_issue_no            = coreIssueNo,
                core_asm_qty_remaining       = coreQtyRemaining,
                upper_tank_asm_issue_no      = upperTankIssueNo,
                upper_tank_asm_qty_remaining = upperTankQtyRemaining,
                lower_tank_asm_issue_no      = lowerTankIssueNo,
                lower_tank_asm_qty_remaining = lowerTankQtyRemaining
            }
        };

        await _mqttPublisher.PublishAsync("data/process/clinching-short-side/process-scan", mqttPayload, cancellationToken);

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

                var (valNum, valText, valBool, status) = ParseParameterValue(param, kvp.Value, defaultStatus: true);
                paramValues.Add((kvp.Key, valNum, valText, valBool, status));
            }
        }

        // Tentukan apakah status proses adalah OK atau NG murni berdasarkan evaluasi parameter
        bool isOk = !paramValues.Any(p => p.status == false || p.valueBoolean == false);

        // Simpan log proses dan detailnya ke database menggunakan repo
        var processLog = await _processLogRepository.AddProcessLogPerProcessAsync(
            request.SerialNumber,
            request.ProcessCode,
            isOk,
            paramValues,
            cancellationToken);

        await SyncMqttPublisherStatusAsync(request.SerialNumber, cancellationToken);

        // Ambil log yang baru dibuat beserta relasi detailnya untuk dikembalikan sebagai DTO
        return await GetProcessLogByIdAsync(processLog.Id, cancellationToken);
    }

    public Task<ProcessLogDto> CreateProcessLogDetailOnlyAsync(
        CreateProcessLogRequestDto request,
        CancellationToken cancellationToken = default)
    {
        return CreateProcessLogDetailOnlyAsync(request, recordHistory: true, cancellationToken);
    }

    public async Task<ProcessLogDto> CreateProcessLogDetailOnlyAsync(
        CreateProcessLogRequestDto request,
        bool recordHistory,
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

        // 3. Get Process Log (Active) with details
        var processLog = await _processLogRepository.GetLogBySerialNumberAsync(
            serialNumber.SerialNumberCode, cancellationToken);

        bool isLastProcess = string.Equals(request.ProcessCode?.Trim(), "HE_LEAK", StringComparison.OrdinalIgnoreCase) || 
                             string.Equals(request.ProcessCode?.Trim(), "M_FAN_INSPECTION", StringComparison.OrdinalIgnoreCase) || 
                             string.Equals(request.ProcessCode?.Trim(), "FINAL_INSPECTION", StringComparison.OrdinalIgnoreCase);

        bool shouldFinish = request.IsFInihed || isLastProcess;

        if (processLog == null)
        {
            // If not found, create new Process Log first
            processLog = new ProcessLog
            {
                SerialNumberId = serialNumber.Id,
                IsActive = true,
                Status = true,
                IsFinished = shouldFinish,
                CreatedAt = DateTime.UtcNow
            };
            await _processLogRepository.AddAsync(processLog, cancellationToken);
            await _processLogRepository.SaveChangesAsync(cancellationToken);
        }
        else
        {
            processLog.UpdatedAt = DateTime.UtcNow;
            processLog.IsFinished = shouldFinish;
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

                var (valNum, valText, valBool, status) = ParseParameterValue(param, kvp.Value, defaultStatus: true);
                paramValues.Add((kvp.Key, valNum, valText, valBool, status));
            }
        }

        // 5. Insert or Update (Replace) details to the ProcessLog entity's Details collection
        var newHistories = recordHistory ? new List<ProcessHistory>() : null;
        foreach (var param in paramValues)
        {
            var parameter = existingParams.FirstOrDefault(x => x.Code == param.parameterCode);
            if (parameter == null) continue;

            var existingDetail = processLog.Details.FirstOrDefault(d => d.ProcessId == process.Id && d.ParameterId == parameter.Id);
            if (existingDetail != null)
            {
                existingDetail.ValueNumber = param.valueNumber;
                existingDetail.ValueText = param.valueText;
                existingDetail.ValueBoolean = param.valueBoolean;
                existingDetail.Status = param.status;
                existingDetail.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                var detail = new ProcessLogDetail
                {
                    ProcessLogId = processLog.Id,
                    ProcessId = process.Id,
                    ParameterId = parameter.Id,
                    ValueNumber = param.valueNumber,
                    ValueText = param.valueText,
                    ValueBoolean = param.valueBoolean,
                    Status = param.status,
                    CreatedAt = DateTime.UtcNow
                };
                processLog.Details.Add(detail);
            }

            if (recordHistory && newHistories != null)
            {
                // Catat ke riwayat (history) baik saat replace (update) maupun insert baru
                var historyValue = param.valueNumber?.ToString(CultureInfo.InvariantCulture)
                                   ?? param.valueText
                                   ?? param.valueBoolean?.ToString()
                                   ?? (request.Data != null && request.Data.TryGetValue(param.parameterCode, out var rawVal) ? rawVal?.ToString() : null);

                var history = new ProcessHistory
                {
                    ReferenceType = ProcessHistoryReferenceTypes.ProcessLog,
                    ReferenceId = processLog.Id,
                    ReferenceCode = serialNumber.SerialNumberCode,
                    ProcessLogId = processLog.Id,
                    SerialNumberId = serialNumber.Id,
                    ProcessId = process.Id,
                    ParameterId = parameter.Id,
                    Value = historyValue,
                    CreatedAt = request.Timestamp ?? DateTime.UtcNow
                };
                newHistories.Add(history);
            }
        }

        if (recordHistory && newHistories != null && newHistories.Count > 0)
        {
            await _processLogRepository.AddHistoriesAsync(newHistories, cancellationToken);
        }

        // 6. Evaluasi ulang status ProcessLog berdasarkan SEMUA parameter yang ada di Details (Parent + Child Relations):
        // - Status = false (NG / 0) jika ada SALAH SATU parameter bernilai NG (Status == false, ValueBoolean == false, atau TypeValue "ok_ng" bernilai 2).
        // - Status = true (OK / 1) jika SEMUA parameter bernilai OK / tidak ada yang NG.
        bool hasNgDetail = processLog.Details.Any(d =>
            d.Status == false ||
            d.ValueBoolean == false ||
            (d.Parameter != null && string.Equals(d.Parameter.TypeValue, "ok_ng", StringComparison.OrdinalIgnoreCase) && d.ValueNumber == 2) ||
            (existingParams.FirstOrDefault(p => p.Id == d.ParameterId) is { } ep && string.Equals(ep.TypeValue, "ok_ng", StringComparison.OrdinalIgnoreCase) && d.ValueNumber == 2)
        );

        bool hasNgInChild = false;
        if (serialNumber.ParentRelations != null && serialNumber.ParentRelations.Count > 0)
        {
            var childDetails = serialNumber.ParentRelations
                .SelectMany(r => r.ChildSerialNumber?.ProcessLogs?.SelectMany(pl => pl.Details) ?? Enumerable.Empty<ProcessLogDetail>())
                .ToList();

            hasNgInChild = childDetails.Any(d =>
                d.Status == false ||
                d.ValueBoolean == false ||
                (d.Parameter != null && string.Equals(d.Parameter.TypeValue, "ok_ng", StringComparison.OrdinalIgnoreCase) && d.ValueNumber == 2)
            );
        }

        processLog.Status = !(hasNgDetail || hasNgInChild);

        _processLogRepository.Update(processLog);
        await _processLogRepository.SaveChangesAsync(cancellationToken);

        await SyncMqttPublisherStatusAsync(request.SerialNumber, cancellationToken);

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

        // 3. Mapping issue numbers ke parameters
        var mappedMfanParams = await _issueService.MapMFanIssuesToParametersAsync(issueNumbers, cancellationToken);

        // 4. Pastikan detail disimpan ke MF serial number
        request.Data ??= new Dictionary<string, object>();
        request.ProcessCode = "M_FAN_ASSY";
        foreach (var kvp in mappedMfanParams)
        {
            request.Data[kvp.Key] = kvp.Value;
        }
        
        request.SerialNumber = mfSerialNumberCode;
        
        var result = await CreateProcessLogDetailOnlyAsync(request, recordHistory: false, cancellationToken);

        // 5. Print barcode / label M-Fan Assy
        int isPrinted = 0;
        try
        {
            await _printService.PrintMFanAssyAsync(mfSerialNumberCode, issueNumbers, cancellationToken);
            isPrinted = 1;
        }
        catch
        {
            isPrinted = 2;
        }

        // 6. Publish MQTT ke topic data/process/m-fan-assy/process-scan
        var fanAsmIssueNo      = mappedMfanParams.TryGetValue("FAN_ASM_RESULT", out var f) ? f?.ToString() : (issueNumbers.Count > 0 ? issueNumbers[0] : null);
        var fanMotorAsmIssueNo = mappedMfanParams.TryGetValue("MOTOR_ASM_RESULT", out var m) ? m?.ToString() : (issueNumbers.Count > 1 ? issueNumbers[1] : null);
        var fanGuideAsmIssueNo = mappedMfanParams.TryGetValue("GUIDE_ASM_RESULT", out var g) ? g?.ToString() : (issueNumbers.Count > 2 ? issueNumbers[2] : null);

        var fanAsmQtyRemaining      = fanAsmIssueNo      != null ? await _issueRepository.GetFinalStockByIssueNumberAsync(fanAsmIssueNo,      cancellationToken) : null;
        var fanMotorAsmQtyRemaining = fanMotorAsmIssueNo != null ? await _issueRepository.GetFinalStockByIssueNumberAsync(fanMotorAsmIssueNo, cancellationToken) : null;
        var fanGuideAsmQtyRemaining = fanGuideAsmIssueNo != null ? await _issueRepository.GetFinalStockByIssueNumberAsync(fanGuideAsmIssueNo, cancellationToken) : null;

        var mqttPayload = new
        {
            serial_number     = mfSerialNumberCode,
            operator_username = request.OperatorUsername ?? "MQTT_M_FAN_ASSY",
            is_printed        = isPrinted,
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

        return result;
    }

    public async Task<ProcessLogDto> CreateProcessLogEcmAssyAsync(
        CreateProcessLogRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var clinchingSnCode = !string.IsNullOrWhiteSpace(request.SerialNumberClinching)
            ? request.SerialNumberClinching
            : request.SerialNumber;

        if (string.IsNullOrWhiteSpace(clinchingSnCode))
            throw new AppException("Serial number clinching (CC) is required.", 400);

        // Simpan process log detail untuk ECM_ASSY pada serial number CC
        request.SerialNumber = clinchingSnCode;
        request.ProcessCode = "ECM_ASSY";
        return await CreateProcessLogDetailOnlyAsync(request, cancellationToken);
    }

    public async Task<ProcessLogDto> CreateProcessLogFinalInspectionAsync(
        CreateProcessLogRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var clinchingSnCode = !string.IsNullOrWhiteSpace(request.SerialNumberClinching)
            ? request.SerialNumberClinching
            : request.SerialNumber;

        if (string.IsNullOrWhiteSpace(clinchingSnCode))
            throw new AppException("Serial number clinching (CC) is required.", 400);

        // Simpan process log detail untuk FINAL_INSPECTION pada serial number CC
        request.SerialNumber = clinchingSnCode;
        request.ProcessCode = "FINAL_INSPECTION";
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
            if (element.ValueKind == System.Text.Json.JsonValueKind.Number)
            {
                if (element.TryGetInt64(out var n))
                {
                    if (n == 1) return true;
                    if (n == 0) return false;
                }
                return null;
            }

            var strVal = (element.ValueKind == System.Text.Json.JsonValueKind.String ? element.GetString() : element.GetRawText())?.Trim().ToUpperInvariant();
            if (strVal == "OK" || strVal == "TRUE" || strVal == "1") return true;
            if (strVal == "NG" || strVal == "FALSE" || strVal == "0") return false;
            return null;
        }

        if (val is bool b) return b;
        if (val is int intVal)
        {
            if (intVal == 1) return true;
            if (intVal == 0) return false;
            return null;
        }

        var str = val.ToString()?.Trim().ToUpperInvariant();
        if (str == "OK" || str == "TRUE" || str == "1") return true;
        if (str == "NG" || str == "FALSE" || str == "0") return false;

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

    private static (decimal? valNum, string? valText, bool? valBool, bool status) ParseParameterValue(
        Parameter param,
        object? rawValue,
        bool defaultStatus)
    {
        decimal? valNum = null;
        string? valText = ParseText(rawValue);
        bool? valBool = null;
        bool status = defaultStatus;

        if (param.DataType == "boolean")
        {
            valBool = ParseBoolean(rawValue);
        }
        else if (param.DataType == "number")
        {
            valNum = ParseDecimal(rawValue);
        }

        // Pengecekan parameter, jika type value-nya ok_ng:
        // 0 -> data belum masuk (valNum = 0, valBool = null, status = defaultStatus)
        // 1 -> ok (valNum = 1, valBool = true, status = true)
        // 2 -> ng (valNum = 2, valBool = false, status = false)
        if (string.Equals(param.TypeValue, "ok_ng", StringComparison.OrdinalIgnoreCase))
        {
            var rawStr = valText?.Trim().ToUpperInvariant() ?? "";
            if (decimal.TryParse(rawStr.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var num))
            {
                if (num == 1)
                {
                    status = true;
                    valBool = true;
                    valNum = 1;
                }
                else if (num == 2)
                {
                    status = false;
                    valBool = false;
                    valNum = 2;
                }
                else if (num == 0)
                {
                    status = defaultStatus;
                    valBool = null;
                    valNum = 0;
                }
            }
            else if (rawStr == "1" || rawStr == "OK" || rawStr == "TRUE" || rawStr == "PASSED")
            {
                status = true;
                valBool = true;
                valNum = 1;
            }
            else if (rawStr == "2" || rawStr == "NG" || rawStr == "FALSE" || rawStr == "REJECTED" || rawStr == "FAIL" || rawStr == "FAILED")
            {
                status = false;
                valBool = false;
                valNum = 2;
            }
            else if (rawStr == "0")
            {
                status = defaultStatus;
                valBool = null;
                valNum = 0;
            }
        }
        else
        {
            if (valBool == false || !defaultStatus)
            {
                status = false;
            }
        }

        return (valNum, valText, valBool, status);
    }

    public static bool? DetermineHistoryStatus(Parameter? param, string? value)
    {
        if (param == null || string.IsNullOrWhiteSpace(value)) return null;

        var valStr = value.Trim().ToUpperInvariant();
        if (string.Equals(param.TypeValue, "ok_ng", StringComparison.OrdinalIgnoreCase))
        {
            if (valStr == "1" || valStr == "OK" || valStr == "TRUE" || valStr == "PASSED") return true;
            if (valStr == "2" || valStr == "NG" || valStr == "FALSE" || valStr == "REJECTED" || valStr == "0") return false;
        }

        if (string.Equals(param.DataType, "boolean", StringComparison.OrdinalIgnoreCase))
        {
            if (valStr == "1" || valStr == "OK" || valStr == "TRUE") return true;
            if (valStr == "0" || valStr == "NG" || valStr == "FALSE") return false;
        }

        return null;
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

    private async Task SyncMqttPublisherStatusAsync(string serialNumberCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(serialNumberCode)) return;

        try
        {
            var masterProcesses = await _processRepository.GetAllAsync(cancellationToken);
            if (masterProcesses == null || !masterProcesses.Any()) return;

            bool isParent = serialNumberCode.StartsWith("PVRA", StringComparison.OrdinalIgnoreCase) ||
                            serialNumberCode.StartsWith("CC", StringComparison.OrdinalIgnoreCase);

            var relevantProcesses = isParent
                ? masterProcesses.Where(p => p.IsActive && (IsClinchingProcess(p.Code) || OverallProcessCodes.Contains(p.Code)))
                : masterProcesses.Where(p => p.IsActive && IsMFanProcess(p.Code));

            await _mqttPublisherStatusRepository.SyncDisabledProcessesAsync(
                serialNumberCode,
                MqttPublisherReferenceTypes.ProcessLog,
                relevantProcesses,
                cancellationToken);
        }
        catch
        {
            // Abaikan agar tidak mengganggu transaksi utama pembuatan log proses
        }
    }
}
