using Mapster;
using TraceabilitySystem.Application.DTOs.ProcessLog;
using TraceabilitySystem.Application.DTOs.SerialNumber;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Domain.Entities;
using TraceabilitySystem.Domain.Interfaces;
using TraceabilitySystem.Shared.Exceptions;
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

    public ProcessLogService(
        IProcessLogRepository processLogRepository,
        IIssueRepository issueRepository,
        IStockInRepository stockInRepository,
        ISerialNumberRepository serialNumberRepository,
        IParameterRepository parameterRepository,
        IProcessRepository processRepository,
        ISerialNumberService serialNumberService,
        IMqttPublisher mqttPublisher)
    {
        _processLogRepository = processLogRepository;
        _issueRepository = issueRepository;
        _stockInRepository = stockInRepository;
        _serialNumberRepository = serialNumberRepository;
        _parameterRepository = parameterRepository;
        _processRepository = processRepository;
        _serialNumberService = serialNumberService;
        _mqttPublisher = mqttPublisher;
    }

    public async Task<PagedResult<ProcessLogListDto>> GetProcessLogsAsync(
        int page,
        int pageSize,
        string? serialNumberCode = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var (logs, totalCount) = await _processLogRepository.GetPagedLogsAsync(
            page, pageSize, serialNumberCode, isActive, cancellationToken);

        var dtos = logs.Select(log => MapToListDto(log)).ToList();

        return new PagedResult<ProcessLogListDto>
        {
            Items      = dtos,
            TotalCount = totalCount,
            Page       = page,
            PageSize   = pageSize
        };
    }

    public async Task<ProcessLogDto> GetProcessLogByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var log = await _processLogRepository.GetLogWithDetailsAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(ProcessLog), id);

        return MapToDto(log);
    }

    public async Task<ProcessLogDto> GetProcessLogBySerialNumberAsync(string serialNumber, CancellationToken cancellationToken = default)
    {
        var log = await _processLogRepository.GetLogBySerialNumberAsync(serialNumber, cancellationToken);
        if (log == null) throw new NotFoundException(nameof(ProcessLog), serialNumber);

        return MapToDto(log);
    }

    // ── Private helpers ─────────────────────────────────────────────────────────

    private static ProcessLogListDto MapToListDto(ProcessLog log)
    {
        var sn = log.SerialNumber;
        if (sn == null) return new ProcessLogListDto();

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

        return new ProcessLogListDto
        {
            Id               = log.Id,
            IsParent         = isParent,
            Status           = log.Status,
            SerialNumberCode = sn.SerialNumberCode,
            Type             = sn.Type,
            CreatedAt        = log.CreatedAt,
            UpdatedAt        = log.UpdatedAt,
            Issues           = issues,
            Processes        = MapDetails(allDetails)
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
        var issueNumbers = new List<string>();
        if (request.Data != null)
        {
            var dataInsensitive = new Dictionary<string, object>(request.Data, StringComparer.OrdinalIgnoreCase);

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
        }

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

        // 3. Set serial number ke request dan panggil CreateProcessLogWithDetailsAsync
        request.SerialNumber = clinchingSerialNumberCode;
        if (string.IsNullOrWhiteSpace(request.ProcessCode))
        {
            request.ProcessCode = "CLINCHING_SHORT_SIDE";
        }

        // 4. Mapping IsOk ke parameter codes yang sesuai dengan value true, dikarenakan process log pertama dan slalu true, karena dari PLC sudah di control
        request.Data ??= new Dictionary<string, object>();
        if (request.IsOk.HasValue)
        {
            request.Data["CORE_ASM_RESULT"]       = true;
            request.Data["UPPER_TANK_ASM_RESULT"]  = true;
            request.Data["LOWER_TANK_ASM_RESULT"]  = true;
        }

        return await CreateProcessLogWithDetailsAsync(request, cancellationToken);
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
        if (string.IsNullOrWhiteSpace(request.SerialNumber))
            throw new AppException("Serial number (CC) is required.", 400);

        // 1. Ambil parent serial number CC dari DB
        var parentSerialNumber = await _serialNumberRepository.FirstOrDefaultAsync(
            x => x.SerialNumberCode == request.SerialNumber, cancellationToken);

        if (parentSerialNumber == null)
            throw new AppException($"Serial number '{request.SerialNumber}' not found.", 404);

        // 2. Ekstrak issue numbers dari request.Data
        //    Bisa berupa array "issue_numbers" atau individual keys: fan_asm_issue_no, fan_motor_asm_issue_no, fan_guide_asm_issue_no

        var issueNumbers = new List<string>();
        if (request.Data != null)
        {
            var dataInsensitive = new Dictionary<string, object>(request.Data, StringComparer.OrdinalIgnoreCase);

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
            }

            if (issueNumbers.Count == 0)
            {
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
        }

        // 3. Generate MF serial number (Qty=1, semua issue dikaitkan, qty issue dikurangi 1)
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

        // 4. Load child MF serial number dari DB untuk mendapatkan Id-nya
        var childSerialNumber = await _serialNumberRepository.FirstOrDefaultAsync(
            x => x.SerialNumberCode == mfSerialNumberCode, cancellationToken);
        if (childSerialNumber == null)
            throw new AppException($"MF serial number '{mfSerialNumberCode}' tidak ditemukan setelah dibuat.", 500);

        // 5. Buat relasi SerialNumberRelation: CC = parent, MF = child
        var relation = new SerialNumberRelation
        {
            ParentSerialNumberId = parentSerialNumber.Id,
            ChildSerialNumberId  = childSerialNumber.Id,
            CreatedAt            = DateTime.UtcNow,
            CreatedBy            = request.OperatorUsername ?? "MQTT_M_FAN_ASSY"
        };
        parentSerialNumber.ParentRelations.Add(relation);
        _serialNumberRepository.Update(parentSerialNumber);
        await _serialNumberRepository.SaveChangesAsync(cancellationToken);

        // 6. Publish MQTT ke topic data/process/m-fan-assy/process
        var fanAsmIssueNo      = issueNumbers.Count > 0 ? issueNumbers[0] : null;
        var fanMotorAsmIssueNo = issueNumbers.Count > 1 ? issueNumbers[1] : null;
        var fanGuideAsmIssueNo = issueNumbers.Count > 2 ? issueNumbers[2] : null;

        var mqttPayload = new
        {
            serial_number     = mfSerialNumberCode,
            operator_username = request.OperatorUsername ?? "MQTT_M_FAN_ASSY",
            timestamp         = DateTime.UtcNow,
            data = new
            {
                fan_asm_issue_no       = fanAsmIssueNo,
                fan_motor_asm_issue_no = fanMotorAsmIssueNo,
                fan_guide_asm_issue_no = fanGuideAsmIssueNo
            }
        };

        await _mqttPublisher.PublishAsync("data/process/m-fan-assy/process-scan", mqttPayload, cancellationToken);

        // 7. Simpan process log details dengan serial number CC (parent) sebagai referensi
        //    Petakan input data ke parameter code database yang sesuai:
        //    LOT_FAN_ASM_RESULT, LOT_MOTOR_ASM_RESULT, LOT_GUIDE_ASM_RESULT, BOLT_TIGHTEN_RESULT, BOLT_TIGHTEN_VALUE, NUT_TIGHTEN_RESULT
        var mappedData = new Dictionary<string, object>();

        if (issueNumbers.Count > 0) mappedData["LOT_FAN_ASM_RESULT"] = issueNumbers[0];
        if (issueNumbers.Count > 1) mappedData["LOT_MOTOR_ASM_RESULT"] = issueNumbers[1];
        if (issueNumbers.Count > 2) mappedData["LOT_GUIDE_ASM_RESULT"] = issueNumbers[2];

        // if (request.Data != null)
        // {
        //     var dataInsensitive = new Dictionary<string, object>(request.Data, StringComparer.OrdinalIgnoreCase);

        //     if (dataInsensitive.TryGetValue("BOLT_TIGHTEN_RESULT", out var boltResult))
        //         mappedData["BOLT_TIGHTEN_RESULT"] = boltResult;

        //     if (dataInsensitive.TryGetValue("BOLT_TIGHTEN_VALUE", out var boltVal))
        //         mappedData["BOLT_TIGHTEN_VALUE"] = boltVal;

        //     if (dataInsensitive.TryGetValue("NUT_TIGHTEN_RESULT", out var nutResult))
        //         mappedData["NUT_TIGHTEN_RESULT"] = nutResult;
        // }

       
        request.Data ??= new Dictionary<string, object>();
        request.ProcessCode = "M_FAN_ASSY";
        if (request.IsOk.HasValue)
        {
            request.Data["CORE_ASM_RESULT"] = true;
            request.Data["UPPER_TANK_ASM_RESULT"] = true;
            request.Data["LOWER_TANK_ASM_RESULT"] = true;
        }
        
        // 8. Pastikan detail disimpan ke child (MF) serial number
        request.SerialNumber = mfSerialNumberCode;
        
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
}
