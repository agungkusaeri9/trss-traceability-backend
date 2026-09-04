using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using TraceabilitySystem.Application.DTOs.MqttLog;
using TraceabilitySystem.Application.DTOs.SystemLog;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Shared.Models;

namespace TraceabilitySystem.Infrastructure.Services;

public class LogService : ILogService
{
    private readonly IConfiguration _configuration;
    private readonly string _loggingConnectionString;

    private static readonly Regex TimestampLevelRegex = new(
        @"^\[(?<timestamp>\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}:\d{2}(?:\.\d{3})?\s+[+-]\d{2}:\d{2})\]\s+\[(?<level>[A-Za-z0-9]{3})\]\s*(?<rest>.*)$",
        RegexOptions.Compiled);

    private static readonly Regex BracketRegex = new(
        @"\[(?<content>[^\]]+)\]",
        RegexOptions.Compiled);

    public LogService(IConfiguration configuration)
    {
        _configuration = configuration;
        _loggingConnectionString = configuration.GetConnectionString("LoggingDatabase")
            ?? throw new InvalidOperationException("Connection string 'LoggingDatabase' not found.");
    }

    #region MQTT Database Logs

    public async Task<PagedResult<MqttMessageLogDto>> GetMqttLogsAsync(
        MqttLogFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var whereClauses = new List<string>();
        var parameters = new List<MySqlParameter>();

        if (!string.IsNullOrWhiteSpace(filter.Topic))
        {
            whereClauses.Add("topic LIKE @topic");
            parameters.Add(new MySqlParameter("@topic", $"%{filter.Topic}%"));
        }

        if (!string.IsNullOrWhiteSpace(filter.ProcessName))
        {
            whereClauses.Add("process_name LIKE @processName");
            parameters.Add(new MySqlParameter("@processName", $"%{filter.ProcessName}%"));
        }

        if (filter.Status.HasValue)
        {
            whereClauses.Add("status = @status");
            parameters.Add(new MySqlParameter("@status", filter.Status.Value.ToString()));
        }

        if (!string.IsNullOrWhiteSpace(filter.OperatorUsername))
        {
            whereClauses.Add("operator_username LIKE @operatorUsername");
            parameters.Add(new MySqlParameter("@operatorUsername", $"%{filter.OperatorUsername}%"));
        }

        if (filter.IsOk.HasValue)
        {
            whereClauses.Add("is_ok = @isOk");
            parameters.Add(new MySqlParameter("@isOk", filter.IsOk.Value));
        }

        if (filter.StartDate.HasValue)
        {
            whereClauses.Add("received_at >= @startDate");
            parameters.Add(new MySqlParameter("@startDate", filter.StartDate.Value));
        }

        if (filter.EndDate.HasValue)
        {
            whereClauses.Add("received_at <= @endDate");
            parameters.Add(new MySqlParameter("@endDate", filter.EndDate.Value));
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            whereClauses.Add("(topic LIKE @search OR process_name LIKE @search OR operator_username LIKE @search OR error_message LIKE @search OR message_id LIKE @search)");
            parameters.Add(new MySqlParameter("@search", $"%{filter.Search}%"));
        }

        var whereSql = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "";
        var countSql = $"SELECT COUNT(*) FROM mqtt_message_logs {whereSql};";

        var page = filter.Page < 1 ? 1 : filter.Page;
        var limit = filter.Limit < 1 ? 10 : filter.Limit;
        var offset = (page - 1) * limit;

        var dataSql = $@"
SELECT 
    id,
    message_id,
    topic,
    process_name,
    operator_username,
    is_ok,
    payload,
    status,
    error_message,
    received_at,
    processed_at,
    created_at,
    updated_at
FROM mqtt_message_logs
{whereSql}
ORDER BY id DESC
LIMIT @limit OFFSET @offset;";

        await using var connection = new MySqlConnection(_loggingConnectionString);
        await connection.OpenAsync(cancellationToken);

        // Count query
        await using var countCmd = new MySqlCommand(countSql, connection);
        foreach (var p in parameters)
        {
            countCmd.Parameters.Add(new MySqlParameter(p.ParameterName, p.Value));
        }
        var totalCountObj = await countCmd.ExecuteScalarAsync(cancellationToken);
        var totalCount = Convert.ToInt32(totalCountObj);

        // Data query
        await using var dataCmd = new MySqlCommand(dataSql, connection);
        foreach (var p in parameters)
        {
            dataCmd.Parameters.Add(new MySqlParameter(p.ParameterName, p.Value));
        }
        dataCmd.Parameters.Add(new MySqlParameter("@limit", limit));
        dataCmd.Parameters.Add(new MySqlParameter("@offset", offset));

        var items = new List<MqttMessageLogDto>();
        await using var reader = await dataCmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(MapMqttLogFromReader(reader));
        }

        return new PagedResult<MqttMessageLogDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = limit
        };
    }

    public async Task<MqttMessageLogDto?> GetMqttLogByIdAsync(ulong id, CancellationToken cancellationToken = default)
    {
        const string sql = @"
SELECT 
    id,
    message_id,
    topic,
    process_name,
    operator_username,
    is_ok,
    payload,
    status,
    error_message,
    received_at,
    processed_at,
    created_at,
    updated_at
FROM mqtt_message_logs
WHERE id = @id
LIMIT 1;";

        await using var connection = new MySqlConnection(_loggingConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new MySqlCommand(sql, connection);
        command.Parameters.Add(new MySqlParameter("@id", id));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return MapMqttLogFromReader(reader);
        }

        return null;
    }

    private static MqttMessageLogDto MapMqttLogFromReader(MySqlDataReader reader)
    {
        var idOrdinal = reader.GetOrdinal("id");
        var msgIdOrdinal = reader.GetOrdinal("message_id");
        var topicOrdinal = reader.GetOrdinal("topic");
        var processNameOrdinal = reader.GetOrdinal("process_name");
        var operatorOrdinal = reader.GetOrdinal("operator_username");
        var isOkOrdinal = reader.GetOrdinal("is_ok");
        var payloadOrdinal = reader.GetOrdinal("payload");
        var statusOrdinal = reader.GetOrdinal("status");
        var errorMsgOrdinal = reader.GetOrdinal("error_message");
        var receivedAtOrdinal = reader.GetOrdinal("received_at");
        var processedAtOrdinal = reader.GetOrdinal("processed_at");
        var createdAtOrdinal = reader.GetOrdinal("created_at");
        var updatedAtOrdinal = reader.GetOrdinal("updated_at");

        return new MqttMessageLogDto
        {
            Id = Convert.ToUInt64(reader.GetValue(idOrdinal)),
            MessageId = reader.GetValue(msgIdOrdinal)?.ToString() ?? string.Empty,
            Topic = reader.GetValue(topicOrdinal)?.ToString() ?? string.Empty,
            ProcessName = reader.IsDBNull(processNameOrdinal) ? null : reader.GetValue(processNameOrdinal)?.ToString(),
            OperatorUsername = reader.IsDBNull(operatorOrdinal) ? null : reader.GetValue(operatorOrdinal)?.ToString(),
            IsOk = reader.IsDBNull(isOkOrdinal) ? null : Convert.ToBoolean(reader.GetValue(isOkOrdinal)),
            Payload = reader.IsDBNull(payloadOrdinal) ? null : reader.GetValue(payloadOrdinal)?.ToString(),
            Status = reader.GetValue(statusOrdinal)?.ToString() ?? string.Empty,
            ErrorMessage = reader.IsDBNull(errorMsgOrdinal) ? null : reader.GetValue(errorMsgOrdinal)?.ToString(),
            ReceivedAt = reader.GetDateTime(receivedAtOrdinal),
            ProcessedAt = reader.IsDBNull(processedAtOrdinal) ? null : reader.GetDateTime(processedAtOrdinal),
            CreatedAt = reader.GetDateTime(createdAtOrdinal),
            UpdatedAt = reader.GetDateTime(updatedAtOrdinal)
        };
    }

    #endregion

    #region System File Logs

    public async Task<PagedResult<SystemLogDto>> GetSystemLogsAsync(
        SystemLogFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var directories = GetLogDirectories();
        var targetDates = ResolveTargetDates(filter);

        var allEntries = new List<SystemLogDto>();

        foreach (var (serviceName, dirPath) in directories)
        {
            if (!Directory.Exists(dirPath))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(filter.Service) &&
                !serviceName.Contains(filter.Service, StringComparison.OrdinalIgnoreCase) &&
                !dirPath.Contains(filter.Service, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (var dateStr in targetDates)
            {
                var filePath = Path.Combine(dirPath, $"{dateStr}.txt");
                if (!File.Exists(filePath))
                {
                    continue;
                }

                var entries = await ReadLogFileAsync(filePath, serviceName, cancellationToken);
                allEntries.AddRange(entries);
            }
        }

        var query = allEntries.AsEnumerable();

        if (filter.Level.HasValue)
        {
            var targetLevel = filter.Level.Value.ToString();
            query = query.Where(e => e.Level.Equals(targetLevel, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(filter.Category))
        {
            query = query.Where(e => e.Category.Contains(filter.Category, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(e =>
                e.Message.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                e.Category.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (e.CorrelationId != null && e.CorrelationId.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                (e.RequestId != null && e.RequestId.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                (e.UserId != null && e.UserId.Contains(search, StringComparison.OrdinalIgnoreCase)));
        }

        bool isAsc = string.Equals(filter.SortOrder, "asc", StringComparison.OrdinalIgnoreCase);
        var sorted = isAsc
            ? query.OrderBy(e => e.Timestamp).ToList()
            : query.OrderByDescending(e => e.Timestamp).ToList();

        var page = filter.Page < 1 ? 1 : filter.Page;
        var limit = filter.Limit < 1 ? 50 : Math.Min(filter.Limit, 500);
        var totalCount = sorted.Count;

        var items = sorted
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToList();

        return new PagedResult<SystemLogDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = limit
        };
    }

    public async Task<SystemLogDto?> GetSystemLogByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        var directories = GetLogDirectories();

        // Optimasi: parse service dan timestamp dari ID (format: {service}_{unixMs}_{lineIndex})
        var lastUnderscore = id.LastIndexOf('_');
        if (lastUnderscore > 0)
        {
            var secondLastUnderscore = id.LastIndexOf('_', lastUnderscore - 1);
            if (secondLastUnderscore > 0)
            {
                string service = id[..secondLastUnderscore];
                string timestampMsStr = id.Substring(secondLastUnderscore + 1, lastUnderscore - secondLastUnderscore - 1);

                if (long.TryParse(timestampMsStr, out var unixMs))
                {
                    var date = DateTimeOffset.FromUnixTimeMilliseconds(unixMs).ToString("yyyy-MM-dd");
                    var matchingDir = directories.FirstOrDefault(d =>
                        d.Key.Equals(service, StringComparison.OrdinalIgnoreCase) ||
                        d.Key.Contains(service, StringComparison.OrdinalIgnoreCase)).Value;

                    if (matchingDir != null)
                    {
                        var filePath = Path.Combine(matchingDir, $"{date}.txt");
                        if (File.Exists(filePath))
                        {
                            var entries = await ReadLogFileAsync(filePath, service, cancellationToken);
                            var found = entries.FirstOrDefault(e => e.Id == id);
                            if (found != null) return found;
                        }
                    }
                }
            }
        }

        // Fallback pencarian jika ID tidak sesuai pola
        foreach (var (serviceName, dirPath) in directories)
        {
            if (!Directory.Exists(dirPath)) continue;

            var files = Directory.GetFiles(dirPath, "*.txt");
            foreach (var file in files.OrderByDescending(f => f))
            {
                var entries = await ReadLogFileAsync(file, serviceName, cancellationToken);
                var found = entries.FirstOrDefault(e => e.Id == id);
                if (found != null)
                {
                    return found;
                }
            }
        }

        return null;
    }

    private Dictionary<string, string> GetLogDirectories()
    {
        var dirs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var explicitDirs = _configuration.GetSection("SystemLogging:Directories");
        if (explicitDirs.Exists())
        {
            foreach (var child in explicitDirs.GetChildren())
            {
                if (!string.IsNullOrWhiteSpace(child.Value))
                {
                    dirs[child.Key] = child.Value;
                }
            }
        }

        if (dirs.Count > 0)
        {
            return dirs;
        }

        string? baseDir = _configuration.GetValue<string>("SystemLogging:BaseDirectory");
        if (string.IsNullOrWhiteSpace(baseDir))
        {
            string? singleLogFolder = _configuration.GetValue<string>("CustomLogging:LogFolder");
            if (!string.IsNullOrWhiteSpace(singleLogFolder))
            {
                baseDir = Path.GetDirectoryName(Path.GetFullPath(singleLogFolder));
            }
        }

        if (!string.IsNullOrWhiteSpace(baseDir) && Directory.Exists(baseDir))
        {
            foreach (var subDir in Directory.GetDirectories(baseDir))
            {
                var folderName = Path.GetFileName(subDir);
                string serviceName = folderName switch
                {
                    "logs-api" => "TraceabilitySystem.API",
                    "logs-worker" => "TraceabilitySystem.Worker",
                    "logs-backup" => "TraceabilitySystem.Backup",
                    _ => folderName
                };

                dirs[serviceName] = subDir;
            }
        }

        return dirs;
    }

    private static List<string> ResolveTargetDates(SystemLogFilterDto filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.Date))
        {
            return new List<string> { filter.Date.Trim() };
        }

        if (!string.IsNullOrWhiteSpace(filter.StartDate) && !string.IsNullOrWhiteSpace(filter.EndDate))
        {
            if (DateTime.TryParse(filter.StartDate, out var start) && DateTime.TryParse(filter.EndDate, out var end))
            {
                if (start > end)
                {
                    (start, end) = (end, start);
                }

                var dates = new List<string>();
                for (var d = start; d <= end; d = d.AddDays(1))
                {
                    dates.Add(d.ToString("yyyy-MM-dd"));
                }
                return dates;
            }
        }

        return new List<string> { DateTime.Now.ToString("yyyy-MM-dd") };
    }

    private static async Task<List<SystemLogDto>> ReadLogFileAsync(
        string filePath,
        string defaultServiceName,
        CancellationToken cancellationToken)
    {
        var entries = new List<SystemLogDto>();
        SystemLogDto? currentEntry = null;
        var fileName = Path.GetFileName(filePath);
        var dirName = Path.GetFileName(Path.GetDirectoryName(filePath) ?? "");
        var sourceLabel = string.IsNullOrEmpty(dirName) ? fileName : $"{dirName}/{fileName}";
        int lineCounter = 0;

        await using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(fileStream, Encoding.UTF8);

        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
        {
            lineCounter++;
            var match = TimestampLevelRegex.Match(line);
            if (match.Success)
            {
                if (currentEntry != null)
                {
                    entries.Add(currentEntry);
                }

                currentEntry = ParseNewSystemLogEntry(match, sourceLabel, defaultServiceName, lineCounter);
            }
            else
            {
                if (currentEntry != null)
                {
                    currentEntry.Message += "\n" + line;
                }
            }
        }

        if (currentEntry != null)
        {
            entries.Add(currentEntry);
        }

        return entries;
    }

    private static SystemLogDto ParseNewSystemLogEntry(
        Match match,
        string sourceLabel,
        string defaultServiceName,
        int lineIndex)
    {
        var timestampStr = match.Groups["timestamp"].Value;
        var level = match.Groups["level"].Value.ToUpperInvariant();
        var rest = match.Groups["rest"].Value;

        if (!DateTimeOffset.TryParse(timestampStr, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedTimestamp))
        {
            parsedTimestamp = DateTimeOffset.Now;
        }

        var bracketMatches = BracketRegex.Matches(rest);
        int lastBracketEnd = 0;
        string service = defaultServiceName;
        string environment = "Unknown";
        string category = "General";
        string? correlationId = null;
        string? requestId = null;
        string? userId = null;

        int metaIndex = 0;
        foreach (Match bm in bracketMatches)
        {
            if (bm.Index > lastBracketEnd + 1)
            {
                break;
            }

            var content = bm.Groups["content"].Value;
            if (content.StartsWith("Corr:", StringComparison.OrdinalIgnoreCase))
            {
                correlationId = content[5..].Trim();
            }
            else if (content.StartsWith("Req:", StringComparison.OrdinalIgnoreCase))
            {
                requestId = content[4..].Trim();
            }
            else if (content.StartsWith("User:", StringComparison.OrdinalIgnoreCase))
            {
                userId = content[5..].Trim();
            }
            else
            {
                if (metaIndex == 0 && !string.IsNullOrWhiteSpace(content)) service = content;
                else if (metaIndex == 1 && !string.IsNullOrWhiteSpace(content)) environment = content;
                else if (metaIndex == 2 && !string.IsNullOrWhiteSpace(content)) category = content;
                metaIndex++;
            }

            lastBracketEnd = bm.Index + bm.Length;
        }

        string message = lastBracketEnd < rest.Length ? rest[lastBracketEnd..].Trim() : string.Empty;

        return new SystemLogDto
        {
            Id = $"{service}_{parsedTimestamp.ToUnixTimeMilliseconds()}_{lineIndex}",
            Timestamp = parsedTimestamp,
            Level = level,
            Service = service,
            Environment = environment,
            Category = category,
            CorrelationId = correlationId,
            RequestId = requestId,
            UserId = userId,
            Message = message,
            SourceFile = sourceLabel
        };
    }

    private static string? NormalizeLevel(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        return input.Trim().ToUpperInvariant() switch
        {
            "INF" or "INFO" or "INFORMATION" => "INF",
            "WRN" or "WARN" or "WARNING" => "WRN",
            "ERR" or "ERROR" => "ERR",
            "DBG" or "DEBUG" => "DBG",
            "FTL" or "FATAL" or "CRITICAL" => "FTL",
            "VRB" or "VERBOSE" or "TRACE" => "VRB",
            _ => input.Trim().ToUpperInvariant()
        };
    }

    #endregion
}
