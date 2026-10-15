using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TraceabilitySystem.Application.DTOs.TraceabilityLog;
using TraceabilitySystem.Domain.Entities;
using TraceabilitySystem.Shared.Helpers;

namespace TraceabilitySystem.Application.Mappers;

public static class TraceabilityLogMapper
{
    private static readonly Regex MultiPointParamRegex =
        new(@"^(CLINCHING_HEIGHT|END_PLATE_WIDTH)(?:_\d+.*)?$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex DigitsRegex =
        new(@"\d+", RegexOptions.Compiled);

    /// <summary>
    public static object? NormalizeValue(object? val)
    {
        if (val == null) return null;
        if (val is decimal dec)
        {
            return dec.ToString("G29", System.Globalization.CultureInfo.InvariantCulture);
        }
        if (val is double dbl)
        {
            return dbl.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        if (val is float flt)
        {
            return flt.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
        if (val is string str)
        {
            var trimmed = str.Trim();
            if (trimmed.Contains(',') || trimmed.Contains('.'))
            {
                if (decimal.TryParse(trimmed.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsedDec))
                {
                    return parsedDec.ToString("G29", System.Globalization.CultureInfo.InvariantCulture);
                }
            }
            return trimmed;
        }
        return val;
    }

    /// <summary>
    /// Helper parsing dan format nilai parameter berdasarkan TypeValue:
    /// - Jika TypeValue == "ok_ng": 0 -> ERROR (Status: false), 1 -> OK (Status: true), 2 -> NG (Status: false)
    /// - Jika TypeValue == "value": return nilai apa adanya tanpa parsing
    /// </summary>
    public static (object? FormattedValue, bool? Status) ParseAndFormatParameterValue(string? typeValue, object? rawValue, bool? dbStatus)
    {
        if (rawValue == null || string.Equals(rawValue.ToString()?.Trim(), "-", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(rawValue.ToString()))
        {
            if (dbStatus == true) return ("OK", true);
            if (dbStatus == false) return ("NG", false);
            return ("-", null);
        }

        if (string.Equals(typeValue, "ok_ng", StringComparison.OrdinalIgnoreCase))
        {
            var s = rawValue.ToString()?.Trim().ToUpperInvariant() ?? "";

            if (decimal.TryParse(s.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var num))
            {
                if (num == 1) return ("OK", true);
                if (num == 2) return ("NG", false);
                if (num == 0) return ("0", dbStatus);
            }

            if (s == "1" || s == "OK" || s == "TRUE" || s == "PASSED")
            {
                return ("OK", true);
            }
            if (s == "2" || s == "NG" || s == "FALSE" || s == "REJECTED" || s == "FAIL" || s == "FAILED")
            {
                return ("NG", false);
            }
            if (s == "0")
            {
                return ("0", dbStatus);
            }

            if (dbStatus.HasValue)
            {
                return (s, dbStatus.Value);
            }
            return (s, true);
        }

        // Jika type_value == "value" atau lainnya, bersihkan angka desimal agar tidak ada trailing zeros atau format koma lokal
        var normalized = NormalizeValue(rawValue);
        return (normalized ?? "-", dbStatus);
    }

    public static List<TraceabilityLogParameterDto> MapDetailsWithMasterParameters(
        IEnumerable<Process>? masterProcesses,
        IEnumerable<ProcessLogDetail>? processLogDetails,
        IEnumerable<TraceabilityLogDetail>? traceabilityLogDetails = null,
        IEnumerable<string>? fallbackIssues = null)
    {
        if (masterProcesses == null || !masterProcesses.Any())
        {
            if (processLogDetails != null && processLogDetails.Any())
                return MapProcessLogDetailsToParameterDtos(processLogDetails, fallbackIssues);
            return MapTraceabilityLogDetailsToParameterDtos(traceabilityLogDetails);
        }

        var pldList = processLogDetails?.ToList() ?? new List<ProcessLogDetail>();
        var tldList = traceabilityLogDetails?.ToList() ?? new List<TraceabilityLogDetail>();
        var matchedPldIds = new HashSet<long>();
        var matchedTldIds = new HashSet<long>();
        var addedParamCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var normalized = new List<(int ProcessOrder, int ParamOrder, long Id, string ParamCode, string? ParamDesc, string TypeValue, object? Value, bool? Status, string RawText)>();

        var orderedProcesses = masterProcesses
            .Where(p => p.IsActive)
            .OrderBy(p => p.Order ?? p.Id)
            .ToList();

        var masterProcIds = new HashSet<int>(orderedProcesses.Select(p => p.Id));
        var masterProcCodes = new HashSet<string>(orderedProcesses.Select(p => p.Code), StringComparer.OrdinalIgnoreCase);

        foreach (var proc in orderedProcesses)
        {
            var procParams = proc.ProcessParameters?
                .Where(pp => pp.Parameter != null && pp.Parameter.IsActive && pp.Parameter.ShowInDisplay)
                .OrderBy(pp => pp.Parameter.Order ?? pp.Parameter.Id)
                .Select(pp => pp.Parameter!)
                .ToList() ?? new List<Parameter>();

            foreach (var param in procParams)
            {
                if (!addedParamCodes.Add(param.Code))
                    continue;

                // Tandai semua record yang cocok di pldList dan tldList
                var pldMatches = pldList.Where(d =>
                    (d.ParameterId == param.Id || string.Equals(d.Parameter?.Code, param.Code, StringComparison.OrdinalIgnoreCase)) &&
                    (d.ProcessId == 0 || d.ProcessId == proc.Id || string.Equals(d.Process?.Code, proc.Code, StringComparison.OrdinalIgnoreCase) || d.Process == null)
                ).ToList();

                foreach (var mp in pldMatches) matchedPldIds.Add(mp.Id);

                var tldMatches = tldList.Where(d =>
                    (d.ParameterId == param.Id || string.Equals(d.Parameter?.Code, param.Code, StringComparison.OrdinalIgnoreCase)) &&
                    (d.ProcessId == 0 || d.ProcessId == proc.Id || string.Equals(d.Process?.Code, proc.Code, StringComparison.OrdinalIgnoreCase) || d.Process == null)
                ).ToList();

                foreach (var mt in tldMatches) matchedTldIds.Add(mt.Id);

                // 1. Cek di processLogDetails (terbaru)
                var bestPld = pldMatches.LastOrDefault();
                if (bestPld != null)
                {
                    object? val = !string.IsNullOrWhiteSpace(bestPld.ValueText)
                        ? NormalizeValue(bestPld.ValueText)
                        : bestPld.ValueNumber.HasValue
                            ? NormalizeValue(bestPld.ValueNumber.Value)
                            : (bestPld.ValueBoolean.HasValue
                                ? (bestPld.ValueBoolean.Value ? "OK" : "NG")
                                : NormalizeValue(bestPld.DisplayValue));

                    normalized.Add((
                        ProcessOrder: proc.Order ?? bestPld.Process?.Order ?? proc.Id,
                        ParamOrder: param.Order ?? bestPld.Parameter?.Order ?? param.Id,
                        Id: bestPld.Id,
                        ParamCode: param.Code,
                        ParamDesc: !string.IsNullOrWhiteSpace(param.Description) ? param.Description : param.Name,
                        TypeValue: param.TypeValue ?? "value",
                        Value: val,
                        Status: (bool?)bestPld.Status,
                        RawText: bestPld.DisplayValue ?? string.Empty
                    ));
                    continue;
                }

                // 2. Cek di traceabilityLogDetails (fallback)
                var bestTld = tldMatches.LastOrDefault();
                if (bestTld != null)
                {
                    normalized.Add((
                        ProcessOrder: proc.Order ?? bestTld.Process?.Order ?? proc.Id,
                        ParamOrder: param.Order ?? bestTld.Parameter?.Order ?? param.Id,
                        Id: bestTld.Id,
                        ParamCode: param.Code,
                        ParamDesc: !string.IsNullOrWhiteSpace(param.Description) ? param.Description : param.Name,
                        TypeValue: param.TypeValue ?? "value",
                        Value: NormalizeValue(bestTld.Value),
                        Status: (bool?)bestTld.Status,
                        RawText: bestTld.Value ?? string.Empty
                    ));
                    continue;
                }

                // 3. Tidak ada di DB -> buat '-'
                normalized.Add((
                    ProcessOrder: proc.Order ?? proc.Id,
                    ParamOrder: param.Order ?? param.Id,
                    Id: 0,
                    ParamCode: param.Code,
                    ParamDesc: !string.IsNullOrWhiteSpace(param.Description) ? param.Description : param.Name,
                    TypeValue: param.TypeValue ?? "value",
                    Value: "-",
                    Status: (bool?)null,
                    RawText: string.Empty
                ));
            }
        }

        var unmatchedPld = pldList.Where(d =>
            !matchedPldIds.Contains(d.Id) &&
            (d.Parameter == null || d.Parameter.ShowInDisplay) &&
            (masterProcIds.Count == 0 || (d.ProcessId > 0 && masterProcIds.Contains(d.ProcessId)) || (d.Process != null && masterProcCodes.Contains(d.Process.Code))));
        foreach (var d in unmatchedPld)
        {
            var code = d.Parameter?.Code ?? $"PARAM_{d.ParameterId}";
            if (!addedParamCodes.Add(code)) continue;

            object? val = !string.IsNullOrWhiteSpace(d.ValueText)
                ? NormalizeValue(d.ValueText)
                : d.ValueNumber.HasValue
                    ? NormalizeValue(d.ValueNumber.Value)
                    : (d.ValueBoolean.HasValue
                        ? (d.ValueBoolean.Value ? "OK" : "NG")
                        : NormalizeValue(d.DisplayValue));

            normalized.Add((
                ProcessOrder: d.Process?.Order ?? int.MaxValue,
                ParamOrder: d.Parameter?.Order ?? int.MaxValue,
                Id: d.Id,
                ParamCode: code,
                ParamDesc: !string.IsNullOrWhiteSpace(d.Parameter?.Description) ? d.Parameter.Description : d.Parameter?.Name,
                TypeValue: d.Parameter?.TypeValue ?? "value",
                Value: val,
                Status: (bool?)d.Status,
                RawText: d.DisplayValue ?? string.Empty
            ));
        }

        var unmatchedTld = tldList.Where(d =>
            !matchedTldIds.Contains(d.Id) &&
            (d.Parameter == null || d.Parameter.ShowInDisplay) &&
            (masterProcIds.Count == 0 || (d.ProcessId > 0 && masterProcIds.Contains(d.ProcessId)) || (d.Process != null && masterProcCodes.Contains(d.Process.Code))));
        foreach (var d in unmatchedTld)
        {
            var code = d.Parameter?.Code ?? $"PARAM_{d.ParameterId}";
            if (!addedParamCodes.Add(code)) continue;

            normalized.Add((
                ProcessOrder: d.Process?.Order ?? int.MaxValue,
                ParamOrder: d.Parameter?.Order ?? int.MaxValue,
                Id: d.Id,
                ParamCode: code,
                ParamDesc: !string.IsNullOrWhiteSpace(d.Parameter?.Description) ? d.Parameter.Description : d.Parameter?.Name,
                TypeValue: d.Parameter?.TypeValue ?? "value",
                Value: NormalizeValue(d.Value),
                Status: (bool?)d.Status,
                RawText: d.Value ?? string.Empty
            ));
        }

        return BuildParameterDtoList(normalized, fallbackIssues);
    }

    public static List<TraceabilityLogParameterDto> MapProcessLogDetailsWithMasterParameters(
        IEnumerable<Process>? masterProcesses,
        IEnumerable<ProcessLogDetail>? loggedDetails,
        IEnumerable<string>? fallbackIssues = null)
        => MapDetailsWithMasterParameters(masterProcesses, loggedDetails, null, fallbackIssues);

    public static List<TraceabilityLogParameterDto> MapProcessLogDetailsToParameterDtos(
        IEnumerable<ProcessLogDetail>? details,
        IEnumerable<string>? fallbackIssues = null)
    {
        if (details == null) return new List<TraceabilityLogParameterDto>();

        var filtered = details.Where(d => d.Parameter == null || d.Parameter.ShowInDisplay);

        var normalized = filtered.Select(d => (
            ProcessOrder: d.Process?.Order ?? int.MaxValue,
            ParamOrder: d.Parameter?.Order ?? int.MaxValue,
            Id: d.Id,
            ParamCode: d.Parameter?.Code ?? $"PARAM_{d.ParameterId}",
            ParamDesc: !string.IsNullOrWhiteSpace(d.Parameter?.Description) ? d.Parameter.Description : d.Parameter?.Name,
            TypeValue: d.Parameter?.TypeValue ?? "value",
            Value: (object?)(!string.IsNullOrWhiteSpace(d.ValueText)
                ? NormalizeValue(d.ValueText)
                : d.ValueNumber.HasValue
                    ? NormalizeValue(d.ValueNumber.Value)
                    : (d.ValueBoolean.HasValue
                        ? (d.ValueBoolean.Value ? "OK" : "NG")
                        : NormalizeValue(d.DisplayValue))),
            Status: (bool?)d.Status,
            RawText: d.DisplayValue ?? string.Empty
        ));

        return BuildParameterDtoList(normalized, fallbackIssues);
    }

    public static List<TraceabilityLogParameterDto> MapTraceabilityLogDetailsToParameterDtos(
        IEnumerable<TraceabilityLogDetail>? details)
    {
        if (details == null) return new List<TraceabilityLogParameterDto>();

        var filtered = details.Where(d => d.Parameter == null || d.Parameter.ShowInDisplay);

        var normalized = filtered.Select(d => (
            ProcessOrder: d.Process?.Order ?? int.MaxValue,
            ParamOrder: d.Parameter?.Order ?? int.MaxValue,
            Id: d.Id,
            ParamCode: d.Parameter?.Code ?? $"PARAM_{d.ParameterId}",
            ParamDesc: !string.IsNullOrWhiteSpace(d.Parameter?.Description) ? d.Parameter.Description : d.Parameter?.Name,
            TypeValue: d.Parameter?.TypeValue ?? "value",
            Value: (object?)NormalizeValue(d.Value),
            Status: (bool?)d.Status,
            RawText: d.Value ?? string.Empty
        ));

        return BuildParameterDtoList(normalized);
    }

    public static TraceabilityLogDto MapToTraceabilityLogDto(
        TraceabilityLog x,
        List<string> clinchingIssues,
        List<string> mfanIssues,
        List<TraceabilityLogParameterDto> clinchingList,
        List<TraceabilityLogParameterDto> heLeakList,
        List<TraceabilityLogParameterDto> mfanList,
        List<TraceabilityLogParameterDto> ecmList,
        List<TraceabilityLogParameterDto> finalList)
    {
        return new TraceabilityLogDto
        {
            Id = x.Id,
            Code = x.Code,
            SerialNumberClinching = x.SerialNumberClinching,
            SerialNumberMFan = x.SerialNumberMFan,
            Status = x.Status,
            IsFinish = x.IsFinish,
            CreatedAt = DateTime.SpecifyKind(x.CreatedAt, DateTimeKind.Utc),
            UpdatedAt = x.UpdatedAt.HasValue ? DateTime.SpecifyKind(x.UpdatedAt.Value, DateTimeKind.Utc) : null,
            IssueNumbersClinching = clinchingIssues,
            IssueNumbersMfan = mfanIssues,
            Detail = new TraceabilityLogProcessGroupDto
            {
                Clinching = clinchingList,
                HeLeak = heLeakList,
                MFan = mfanList,
                Ecm = ecmList,
                Final = finalList
            }
        };
    }

    private static List<TraceabilityLogParameterDto> BuildParameterDtoList(
        IEnumerable<(int ProcessOrder, int ParamOrder, long Id, string ParamCode, string? ParamDesc, string TypeValue, object? Value, bool? Status, string RawText)> items,
        IEnumerable<string>? fallbackIssues = null)
    {
        var ordered = items
            .OrderBy(x => x.ProcessOrder)
            .ThenBy(x => x.ParamOrder)
            .ToList();

        var result = new List<TraceabilityLogParameterDto>();
        var groupedPrefixes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var arrayGroups = ordered
            .Select(x => new { Item = x, Match = MultiPointParamRegex.Match(x.ParamCode), Suffix = ExtractNumberSuffix(x.ParamCode) })
            .Where(x => x.Match.Success)
            .GroupBy(x => x.Match.Groups[1].Value.ToUpperInvariant())
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(x => x.Item.ParamOrder)
                      .ThenBy(x => x.Suffix)
                      .Select(x => x.Item)
                      .ToList()
            );

        foreach (var item in ordered)
        {
            var match = MultiPointParamRegex.Match(item.ParamCode);
            if (match.Success)
            {
                var groupKey = match.Groups[1].Value.ToUpperInvariant();
                if (groupedPrefixes.Add(groupKey))
                {
                    var groupDetails = arrayGroups[groupKey];
                    var isAllEmpty = groupDetails.All(g => string.IsNullOrWhiteSpace(g.RawText));

                    var formattedGroup = groupDetails.Select(g => ParseAndFormatParameterValue(g.TypeValue, g.Value, g.Status)).ToList();
                    var groupValues = formattedGroup.Select(g => g.FormattedValue).ToList();
                    var rawGroupDesc = groupDetails.Select(g => g.ParamDesc).FirstOrDefault(d => !string.IsNullOrWhiteSpace(d));
                    var groupDesc = CleanGroupDescription(groupKey, rawGroupDesc);

                    bool? groupStatus = isAllEmpty
                        ? null
                        : formattedGroup.Any(g => g.Status == false) ? false : true;

                    object groupFinalValue = isAllEmpty ? "-" : groupValues;

                    result.Add(new TraceabilityLogParameterDto
                    {
                        Parameter = $"{groupKey}_ALL",
                        ParameterDesc = groupDesc,
                        Value = groupFinalValue,
                        Status = groupStatus
                    });
                }
            }
            else
            {
                var (formattedVal, status) = ParseAndFormatParameterValue(item.TypeValue, item.Value, item.Status);
                if (formattedVal == null || (formattedVal is string st && (string.IsNullOrWhiteSpace(st) || st == "-")))
                {
                    formattedVal = "-";
                    status = null;
                }

                result.Add(new TraceabilityLogParameterDto
                {
                    Parameter = item.ParamCode,
                    ParameterDesc = item.ParamDesc,
                    Value = formattedVal,
                    Status = status
                });
            }
        }

        var finalResult = new List<TraceabilityLogParameterDto>();
        var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in result)
        {
            if (p.Parameter != null && !seenKeys.Add(p.Parameter))
                continue;
            finalResult.Add(p);
        }

        return finalResult;
    }

    private static string CleanGroupDescription(string groupKey, string? rawDesc)
    {
        if (string.IsNullOrWhiteSpace(rawDesc))
        {
            return groupKey.ToUpperInvariant() switch
            {
                "CLINCHING_HEIGHT" => "Clinching Height Result",
                "END_PLATE_WIDTH" => "End Plate Width Result",
                _ => System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(groupKey.Replace('_', ' ').ToLowerInvariant())
            };
        }

        var cleaned = Regex.Replace(rawDesc, @"(?<=\b[A-Za-z]+)\s+\d+\b", string.Empty);
        cleaned = Regex.Replace(cleaned, @"[_\s]+\d+\s*$", string.Empty);
        cleaned = Regex.Replace(cleaned, @"\s{2,}", " ").Trim();

        return cleaned;
    }

    private static int ExtractNumberSuffix(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return int.MaxValue;
        var match = DigitsRegex.Match(text);
        return match.Success && int.TryParse(match.Value, out var n) ? n : int.MaxValue;
    }
}
