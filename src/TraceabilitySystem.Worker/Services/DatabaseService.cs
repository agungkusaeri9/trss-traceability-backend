using System.Data;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace TraceabilitySystem.Worker.Services;

public class DatabaseService
{
    private readonly string _connectionString;
    private readonly ILogger<DatabaseService> _logger;

    public DatabaseService(IConfiguration configuration, ILogger<DatabaseService> logger)
    {
        _connectionString = configuration.GetConnectionString("LoggingDatabase")
            ?? throw new InvalidOperationException("Connection string 'LoggingDatabase' not found.");
        _logger = logger;
    }

    /// <summary>
    /// Menjamin payload adalah string JSON yang valid agar fungsi CAST(@payload AS JSON) pada MySQL tidak error.
    /// </summary>
    private static string EnsureValidJson(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return "{}";
        }

        try
        {
            using var doc = JsonDocument.Parse(payload);
            return payload;
        }
        catch
        {
            return JsonSerializer.Serialize(new { raw = payload });
        }
    }

    /// <summary>
    /// Menyimpan pesan MQTT mentah ke tabel mqtt_message_logs.
    /// </summary>
    public async Task<string> SaveMqttMessageAsync(
        string messageId,
        string topic,
        string payload,
        string? operatorUsername = null,
        bool? isOk = null,
        string status = "RECEIVED",
        string? processName = null,
        string? errorMessage = null)
    {
        var jsonPayload = EnsureValidJson(payload);

        const string sql = @"
INSERT INTO mqtt_message_logs
(
    message_id,
    topic,
    process_name,
    operator_username,
    is_ok,
    payload,
    status,
    error_message,
    received_at
)
VALUES
(
    @messageId,
    @topic,
    @processName,
    @operatorUsername,
    @isOk,
    CAST(@payload AS JSON),
    @status,
    @errorMessage,
    NOW(3)
);";

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new MySqlCommand(sql, connection);

        command.Parameters.Add("@messageId", MySqlDbType.VarChar).Value = messageId;
        command.Parameters.Add("@topic", MySqlDbType.VarChar).Value = topic;
        command.Parameters.Add("@processName", MySqlDbType.VarChar).Value = (object?)processName ?? DBNull.Value;
        command.Parameters.Add("@operatorUsername", MySqlDbType.VarChar).Value = (object?)operatorUsername ?? DBNull.Value;
        command.Parameters.Add("@isOk", MySqlDbType.Bool).Value = (object?)isOk ?? DBNull.Value;
        command.Parameters.Add("@payload", MySqlDbType.JSON).Value = jsonPayload;
        command.Parameters.Add("@status", MySqlDbType.VarChar).Value = status;
        command.Parameters.Add("@errorMessage", MySqlDbType.Text).Value = (object?)errorMessage ?? DBNull.Value;

        await command.ExecuteNonQueryAsync();
        return messageId;
    }

    /// <summary>
    /// Memperbarui status pemrosesan pesan MQTT beserta pesan error dan waktu selesai (processed_at).
    /// </summary>
    public async Task UpdateMqttMessageStatusAsync(
        string messageId,
        string status,
        string? errorMessage = null)
    {
        const string sql = @"
UPDATE mqtt_message_logs
SET
    status = @status,
    error_message = @errorMessage,
    processed_at = NOW(3),
    updated_at = NOW(3)
WHERE message_id = @messageId;";

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new MySqlCommand(sql, connection);

        command.Parameters.Add("@messageId", MySqlDbType.VarChar).Value = messageId;
        command.Parameters.Add("@status", MySqlDbType.VarChar).Value = status;
        command.Parameters.Add("@errorMessage", MySqlDbType.Text).Value = (object?)errorMessage ?? DBNull.Value;

        await command.ExecuteNonQueryAsync();
    }
}