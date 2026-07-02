using System.Data;
using Microsoft.Extensions.Configuration;
using MySqlConnector;
using Org.BouncyCastle.Asn1.Ocsp;
using TraceabilitySystem.Application.DTOs.ProcessLog;

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

    public async Task SaveMqttMessageAsync(
        string topic,
        string payload,
        string? operatorUsername,
        bool? isOk,
        string status,
        string? processName = null,
        string? errorMessage = null)
    {
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
    UUID(),
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

        command.Parameters.Add("@topic", MySqlDbType.VarChar).Value = topic;
        command.Parameters.Add("@processName", MySqlDbType.VarChar).Value = (object?)processName ?? DBNull.Value;
        command.Parameters.Add("@operatorUsername", MySqlDbType.VarChar).Value = (object?)operatorUsername ?? DBNull.Value;
        command.Parameters.Add("@isOk", MySqlDbType.Bool).Value = (object?)isOk ?? DBNull.Value;
        command.Parameters.Add("@payload", MySqlDbType.JSON).Value = payload;
        command.Parameters.Add("@status", MySqlDbType.VarChar).Value = status;
        command.Parameters.Add("@errorMessage", MySqlDbType.Text).Value = (object?)errorMessage ?? DBNull.Value;

        await command.ExecuteNonQueryAsync();
    }

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
    updated_at = NOW(3)
WHERE message_id = @messageId;";

        await using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new MySqlCommand(sql, connection);

        command.Parameters.Add("@messageId", MySqlDbType.VarChar).Value = messageId;
        command.Parameters.Add("@status", MySqlDbType.VarChar).Value = status;
        command.Parameters.Add("@errorMessage", MySqlDbType.Text).Value =
            (object?)errorMessage ?? DBNull.Value;

        await command.ExecuteNonQueryAsync();
    }


    public async Task SaveProcessClinchingShortSideAsync(string? errorMessage,string payload, CreateProcessLogRequestDto request)
    {
        try
        {
            await SaveMqttMessageAsync(
                topic: "data/process/clinching-short-side/result",
                processName: "Clinching Short Side",
                payload: payload,
                operatorUsername: request?.OperatorUsername,
                isOk: request?.IsOk,
                errorMessage: errorMessage,
                status: "RECEIVED"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[MQTT][Database] Failed to save Clinching Short Side log.");
        }
        
    }

    public async Task SaveProcessClinchingLongSideAsync(string? errorMessage, string payload, CreateProcessLogRequestDto request)
    {
        try
        {
            await SaveMqttMessageAsync(
                  topic: "data/process/clinching-long-side/result",
                  processName: "Clinching Long Side",
                  payload: payload,
                  operatorUsername: request?.OperatorUsername,
                  isOk: request?.IsOk,
                  errorMessage: errorMessage,
                  status: "RECEIVED"
              );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[MQTT][Database] Failed to save Clinching long Side log.");
        }

       
    }

    public async Task SaveHeLeakAsync(string? errorMessage, string payload, CreateProcessLogRequestDto request)
    {
        try
        {
            await SaveMqttMessageAsync(
               topic: "data/process/he-leak/result",
               processName: "He Leak",
               payload: payload,
               operatorUsername: request?.OperatorUsername,
               isOk: request?.IsOk,
               errorMessage: errorMessage,
               status: "RECEIVED"
           );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[MQTT][Database] Failed to save he leak log.");
        }

        
    }

    public async Task SaveMFanAssyScanAsync(string? errorMessage, string payload, CreateProcessLogRequestDto request)
    {
        
        try
        {
            await SaveMqttMessageAsync(
               topic: "data/process/m-fan-assy/result-scan",
               processName: "M Fan Assy Scan",
               payload: payload,
               operatorUsername: request?.OperatorUsername,
               isOk: request?.IsOk,
               errorMessage: errorMessage,
               status: "RECEIVED"
           );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[MQTT][Database] Failed to save hem fan assy scan log.");
        }
    }

    public async Task SaveMFanAssyAsync(string? errorMessage, string payload, CreateProcessLogRequestDto request)
    {
        try
        {
            await SaveMqttMessageAsync(
               topic: "data/process/m-fan-assy/result",
               processName: "M Fan Assy ",
               payload: payload,
               operatorUsername: request?.OperatorUsername,
               isOk: request?.IsOk,
               errorMessage:errorMessage,
               status: "RECEIVED"
           );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[MQTT][Database] Failed to save hem fan assy log.");
        }
       
    }

    public async Task SaveMFanInspectionAsync(string? errorMessage, string payload, CreateProcessLogRequestDto request)
    {
        try
        {
            await SaveMqttMessageAsync(
                 topic: "data/process/m-fan-inspection/result",
                  processName: "M Fan Inspection ",
                  payload: payload,
                  operatorUsername: request?.OperatorUsername,
                  isOk: request?.IsOk,
                  errorMessage: errorMessage,
                  status: "RECEIVED"
              );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[MQTT][Database] Failed to save hem m fan insepction log.");
        }
       
    }

    public async Task SaveEcmAssynAsync(string? errorMessage, string payload, CreateProcessLogRequestDto request)
    {
       
        try
        {
            await SaveMqttMessageAsync(
              topic: "data/process/ecm-assy/result",
               processName: "ECM Assy",
               payload: payload,
               operatorUsername: request?.OperatorUsername,
               isOk: request?.IsOk,
               errorMessage: errorMessage,
               status: "RECEIVED"
           );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[MQTT][Database] Failed to save hem ecm assy log.");
        }
    }


    public async Task SaveFinalInspectionAsync(string? errorMessage, string payload, CreateProcessLogRequestDto request)
    {
        try
        {
            await SaveMqttMessageAsync(
              topic: "data/process/final-inspection/result",
              processName: "Final Inspection",
              payload: payload,
              operatorUsername: request?.OperatorUsername,
              isOk: request?.IsOk,
              errorMessage: errorMessage,
              status: "RECEIVED"
          );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[MQTT][Database] Failed to save hem final inspection log.");
        }
       
    }




}