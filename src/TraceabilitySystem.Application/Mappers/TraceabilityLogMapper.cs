using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TraceabilitySystem.Application.DTOs.TraceabilityLog;
using TraceabilitySystem.Domain.Entities;

namespace TraceabilitySystem.Application.Mappers;

public static class TraceabilityLogMapper
{
    private static readonly Regex MultiPointParamRegex =
        new(@"^(CLINCHING_HEIGHT|END_PLATE_WIDTH|CHECK_POINT)(?:_\d+.*)?$",
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
        if (string.Equals(typeValue, "ok_ng", StringComparison.OrdinalIgnoreCase))
        {
            if (rawValue == null)
            {
                if (dbStatus == true) return ("OK", true);
                if (dbStatus == false) return ("NG", false);
                return ("-", null);
            }

            var s = rawValue.ToString()?.Trim().ToUpperInvariant() ?? "";

            if (decimal.TryParse(s.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var num))
            {
                if (num == 1) return ("OK", true);
                if (num == 2) return ("NG", false);
                if (num == 0) return ("ERROR", false);
            }

            if (s == "1" || s == "OK" || s == "TRUE" || s == "PASSED")
            {
                return ("OK", true);
            }
            if (s == "2" || s == "NG" || s == "FALSE" || s == "REJECTED" || s == "FAIL" || s == "FAILED")
            {
                return ("NG", false);
            }
            if (s == "0" || s == "ERROR" || s == "ERR")
            {
                return ("ERROR", false);
            }

            if (dbStatus.HasValue)
            {
                return (s, dbStatus.Value);
            }
            return (s, true);
        }

        // Jika type_value == "value" atau lainnya, bersihkan angka desimal agar tidak ada trailing zeros atau format koma lokal
        return (NormalizeValue(rawValue), dbStatus);
    }

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
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt,
            IssueNumbersClinching = clinchingIssues,
            IssueNumbersMfan = mfanIssues,
            Detail = new TraceabilityLogProcessGroupDto
            {
                Clinching = clinchingList,
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
            .ThenBy(x => x.Id)
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
                      .ThenBy(x => x.Item.Id)
                      .Select(x => x.Item)
                      .ToList()
            );

        int issueIndex = 0;
        var issueList = fallbackIssues?.ToList() ?? new List<string>();

        foreach (var item in ordered)
        {
            var match = MultiPointParamRegex.Match(item.ParamCode);
            if (match.Success)
            {
                var groupKey = match.Groups[1].Value.ToUpperInvariant();
                if (groupedPrefixes.Add(groupKey))
                {
                    var groupDetails = arrayGroups[groupKey];
                    var formattedGroup = groupDetails.Select(g => ParseAndFormatParameterValue(g.TypeValue, g.Value, g.Status)).ToList();
                    var groupValues = formattedGroup.Select(g => g.FormattedValue).ToList();
                    var rawGroupDesc = groupDetails.Select(g => g.ParamDesc).FirstOrDefault(d => !string.IsNullOrWhiteSpace(d));
                    var groupDesc = CleanGroupDescription(groupKey, rawGroupDesc);

                    bool? groupStatus = (groupValues.Count == 0 || groupDetails.All(g => string.IsNullOrWhiteSpace(g.RawText)))
                        ? null
                        : formattedGroup.Any(g => g.Status == false) ? false : true;

                    result.Add(new TraceabilityLogParameterDto
                    {
                        Parameter = $"{groupKey}_ALL",
                        ParameterDesc = groupDesc,
                        Value = groupValues,
                        Status = groupStatus
                    });
                }
            }
            else
            {
                var (formattedVal, status) = ParseAndFormatParameterValue(item.TypeValue, item.Value, item.Status);
                if (formattedVal == null || (formattedVal is string s && string.IsNullOrWhiteSpace(s)))
                {
                    if (issueIndex < issueList.Count)
                    {
                        formattedVal = issueList[issueIndex++];
                    }
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

        return result;
    }

    private static string CleanGroupDescription(string groupKey, string? rawDesc)
    {
        if (string.IsNullOrWhiteSpace(rawDesc))
        {
            return groupKey.ToUpperInvariant() switch
            {
                "CLINCHING_HEIGHT" => "Clinching Height Result",
                "END_PLATE_WIDTH" => "End Plate Width Result",
                "CHECK_POINT" => "Check Point",
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
