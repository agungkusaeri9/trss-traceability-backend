
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.SignalR.Client;
using MQTTnet;
using MQTTnet.Client;
using System;
using System.Text;
using System.Text.Json;
using TraceabilitySystem.Application.DTOs.ProcessLog;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Shared.Models;
using TraceabilitySystem.Worker.Services;

namespace TraceabilitySystem.Worker.BackgroundServices;

public class MqttWorkerService : BackgroundService
{
    private readonly ILogger<MqttWorkerService> _logger;
    private readonly MqttSettings _mqttSettings;
    private readonly WorkerSettings _workerSettings;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly MqttClientAccessor _mqttClientAccessor;

    private IMqttClient? _mqttClient;
    private HubConnection? _hubConnection;
    private bool _isConnected;

    public MqttWorkerService(
        ILogger<MqttWorkerService> logger,
        IOptions<MqttSettings> mqttSettings,
        IOptions<WorkerSettings> workerSettings,
        IServiceScopeFactory scopeFactory,
        MqttClientAccessor mqttClientAccessor)
    {
        _logger = logger;
        _mqttSettings = mqttSettings.Value;
        _workerSettings = workerSettings.Value;
        _scopeFactory = scopeFactory;
        _mqttClientAccessor = mqttClientAccessor;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MqttPrintRequestService in Worker started.");

        // Set up SignalR Client Connection
        var hubUrl = $"{_workerSettings.ApiUrl.TrimEnd('/')}/hubs/mqtt-status";
        _logger.LogInformation("Connecting to API SignalR Hub at {HubUrl}...", hubUrl);

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.Reconnected += async (connectionId) =>
        {
            _logger.LogInformation("SignalR connection reconnected. Resending MQTT status: {Status}", _isConnected);
            await SendStatusToHubAsync(_isConnected);
        };

        _hubConnection.Closed += async (error) =>
        {
            while (!stoppingToken.IsCancellationRequested && _hubConnection.State == HubConnectionState.Disconnected)
            {
                try
                {
                    await Task.Delay(5000, stoppingToken);
                    await _hubConnection.StartAsync(stoppingToken);
                    _logger.LogInformation("SignalR connection re-established after close. Resending MQTT status: {Status}", _isConnected);
                    await SendStatusToHubAsync(_isConnected);
                    break;
                }
                catch
                {
                    // keep trying
                }
            }
        };

        try
        {
            await _hubConnection.StartAsync(stoppingToken);
            _logger.LogInformation("Successfully connected to API SignalR Hub.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to connect to API SignalR Hub at {HubUrl}: {Message}. Automatic reconnect will handle subsequent attempts.", hubUrl, ex.Message);
        }

        // Set up MQTT Client Connection
        var factory = new MqttFactory();
        _mqttClient = factory.CreateMqttClient();
        _mqttClientAccessor.Client = _mqttClient;

        _mqttClient.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;
        _mqttClient.ConnectedAsync += OnConnectedAsync;
        _mqttClient.DisconnectedAsync += OnDisconnectedAsync;

        await ConnectMqttAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_mqttClient != null && !_mqttClient.IsConnected)
                {
                    _logger.LogInformation("[MQTT] Client is disconnected. Attempting auto-reconnection to {Broker}:{Port}...", _mqttSettings.Broker, _mqttSettings.Port);
                    await ConnectMqttAsync(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("[MQTT] Error in auto-reconnection loop: {Message}", ex.Message);
            }

            await Task.Delay(5000, stoppingToken);
        }

        if (_mqttClient != null && _mqttClient.IsConnected)
        {
            await _mqttClient.DisconnectAsync(cancellationToken: stoppingToken);
        }

        if (_hubConnection != null)
        {
            await _hubConnection.StopAsync(stoppingToken);
            await _hubConnection.DisposeAsync();
        }
    }

    private async Task SendStatusToHubAsync(bool isConnected)
    {
        if (_hubConnection == null || _hubConnection.State != HubConnectionState.Connected)
        {
            _logger.LogWarning("Cannot send status to API Hub. Hub connection state: {State}", _hubConnection?.State);
            return;
        }

        try
        {
            await _hubConnection.InvokeAsync("UpdateStatus", isConnected);
            _logger.LogInformation("Sent connection status to API Hub: {Status}", isConnected ? "Online" : "Offline");
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to send connection status to API Hub: {Message}", ex.Message);
        }
    }

    private async Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg)
    {
        var correlationId = Guid.NewGuid().ToString("N")[..12];
        using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
        using (Serilog.Context.LogContext.PushProperty("Category", TraceabilitySystem.Shared.Constants.LogCategory.Integration))
        using (Serilog.Context.LogContext.PushProperty("UserId", "PLC/Worker"))
        {
            var messageId = Guid.NewGuid().ToString();
            var topic = arg.ApplicationMessage.Topic;
            var payload = Encoding.UTF8.GetString(arg.ApplicationMessage.PayloadSegment);

            _logger.LogInformation("[MQTT] Received message on topic [{Topic}] (MessageId: {MessageId})", topic, messageId);

            using var scope = _scopeFactory.CreateScope();
            var databaseService = scope.ServiceProvider.GetRequiredService<DatabaseService>();
            var subscriptionService = scope.ServiceProvider.GetRequiredService<MqttSubscriptionService>();
            var mqttPublisher = scope.ServiceProvider.GetRequiredService<IMqttPublisher>();

            var processName = GetProcessNameFromTopic(topic);

            // 1. Ekstrak data dasar payload secara aman jika berupa JSON
            string? operatorUsername = null;
            bool? isOk = null;
            string? parseError = null;

            try
            {
                using var doc = JsonDocument.Parse(payload);
                var root = doc.RootElement;
                if (root.TryGetProperty("operator_username", out var opProp) || root.TryGetProperty("operatorUsername", out opProp))
                    operatorUsername = opProp.GetString();
                if (root.TryGetProperty("is_ok", out var okProp) || root.TryGetProperty("isOk", out okProp))
                {
                    if (okProp.ValueKind == JsonValueKind.True || okProp.ValueKind == JsonValueKind.False)
                        isOk = okProp.GetBoolean();
                }
            }
            catch (Exception ex)
            {
                parseError = $"Invalid JSON payload: {ex.Message}";
                _logger.LogWarning("[MQTT] Failed to deserialize payload for topic [{Topic}]: {Error}", topic, parseError);
            }

            // 2. Selalu simpan pesan mentah MQTT ke database logging (trss_traceability_system_logging.mqtt_message_logs) terlebih dahulu
            var initialStatus = parseError == null ? "RECEIVED" : "FAILED";
            try
            {
                await databaseService.SaveMqttMessageAsync(
                    messageId: messageId,
                    topic: topic,
                    payload: payload,
                    operatorUsername: operatorUsername,
                    isOk: isOk,
                    status: initialStatus,
                    processName: processName,
                    errorMessage: parseError
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MQTT][Database] Failed to save raw MQTT message to database. Topic: {Topic}", topic);
            }

            // Khusus topic konfigurasi MQTT publisher/subscription
            if (topic == "data/process/is-publisher")
            {
                await subscriptionService.HandleProcessSubscriptionAsync(messageId, payload);
                return;
            }

            // 3. Jika payload bukan JSON valid, beri respons error ke broker jika berupa topic result
            if (parseError != null)
            {
                var processKey = GetProcessKeyFromTopic(topic);
                if (processKey != null)
                {
                    var errorPayload = new
                    {
                        status = false,
                        process = processKey,
                        error = parseError
                    };
                    await mqttPublisher.PublishAsync("data/process/validation", errorPayload);
                }
                return;
            }

            CreateProcessLogRequestDto? request = null;
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                request = JsonSerializer.Deserialize<CreateProcessLogRequestDto>(payload, options);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("[MQTT] Failed to deserialize CreateProcessLogRequestDto for topic [{Topic}]: {Error}", topic, ex.Message);
            }

            if (request == null)
            {
                _logger.LogWarning("[MQTT] Request payload deserialized to null for topic [{Topic}]", topic);
                await databaseService.UpdateMqttMessageStatusAsync(messageId, "FAILED", "Deserialized request is null");
                return;
            }

            // 4. Dispatch topic proses ke handler yang sesuai
            Task? processResultTask = topic switch
            {
                "data/process/clinching-short-side/result-scan" => subscriptionService.HandleClinchingShortSideResultScanAsync(messageId, payload, request),
                "data/process/clinching-short-side/result" => subscriptionService.HandleClinchingShortSideResultAsync(messageId, payload, request),
                "data/process/clinching-long-side/result" => subscriptionService.HandleClinchingLongSideResultAsync(messageId, payload, request),
                "data/process/he-leak/result" => subscriptionService.HandleHeLeakResultAsync(messageId, payload, request),
                "data/process/m-fan-assy/result-scan" => subscriptionService.HandleMFanAssyResultScanAsync(messageId, payload, request),
                "data/process/m-fan-assy/result" => subscriptionService.HandleMFanAssyResultAsync(messageId, payload, request),
                "data/process/m-fan-inspection/result" => subscriptionService.HandleMFanInspectionResultAsync(messageId, payload, request),
                "data/process/ecm-assy/result" => subscriptionService.HandleEcmAssyResultAsync(messageId, payload, request),
                "data/process/final-inspection/result" => subscriptionService.HandleFinalInspectionResultAsync(messageId, payload, request),
                _ => null
            };

            if (processResultTask is not null)
            {
                await processResultTask;
                return;
            }

            // Pesan non-process (seperti print request) telah tercatat di DB
            _logger.LogInformation("[MQTT] Topic [{Topic}] received and recorded in database (no process result handler configured).", topic);
        }
    }

    private static string GetProcessNameFromTopic(string topic) => topic switch
    {
        "data/process/is-publisher" => "Process MQTT Publisher Status",
        "data/process/clinching-short-side/result-scan" => "Clinching Short Side Scan",
        "data/process/clinching-short-side/result" => "Clinching Short Side",
        "data/process/clinching-long-side/result" => "Clinching Long Side",
        "data/process/he-leak/result" => "He Leak",
        "data/process/m-fan-assy/result-scan" => "M Fan Assy Scan",
        "data/process/m-fan-assy/result" => "M Fan Assy",
        "data/process/m-fan-inspection/result" => "M Fan Inspection",
        "data/process/ecm-assy/result" => "ECM Assy",
        "data/process/final-inspection/result" => "Final Inspection",
        "traceability/print/request/clinching-short-side" => "Print Request Clinching Short Side",
        "traceability/print/request/m-fan-assy" => "Print Request M Fan Assy",
        _ => topic
    };

    private static string? GetProcessKeyFromTopic(string topic) => topic switch
    {
        "data/process/is-publisher" => "is-publisher",
        "data/process/clinching-short-side/result-scan" => "clinching-short-side-scan",
        "data/process/clinching-short-side/result" => "clinching-short-side",
        "data/process/clinching-long-side/result" => "clinching-long-side",
        "data/process/he-leak/result" => "he-leak",
        "data/process/m-fan-assy/result-scan" => "m-fan-assy-scan",
        "data/process/m-fan-assy/result" => "m-fan-assy",
        "data/process/m-fan-inspection/result" => "m-fan-inspection",
        "data/process/ecm-assy/result" => "ecm-assy",
        "data/process/final-inspection/result" => "final-inspection",
        _ => null
    };

    private async Task OnConnectedAsync(MqttClientConnectedEventArgs arg)
    {
        _logger.LogInformation("Connected to MQTT broker.");
        _isConnected = true;
        await SendStatusToHubAsync(true);

        var topics = new[]
        {
            // Print request topics
            "traceability/print/request/clinching-short-side",
            "traceability/print/request/m-fan-assy",
            // Process status topics
            "data/process/is-publisher",
            // Process result topics
            "data/process/clinching-short-side/result-scan",
            "data/process/clinching-short-side/result",
            "data/process/clinching-long-side/result",
            "data/process/he-leak/result",
            "data/process/m-fan-assy/result-scan",
            "data/process/m-fan-assy/result",
            "data/process/m-fan-inspection/result",
            "data/process/ecm-assy/result",
            "data/process/final-inspection/result"
        };

        var subscribeBuilder = new MqttFactory().CreateSubscribeOptionsBuilder();
        foreach (var topic in topics)
        {
            subscribeBuilder.WithTopicFilter(f => f
                .WithTopic(topic)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce));
        }

        var mqttSubscribeOptions = subscribeBuilder.Build();

        await _mqttClient!.SubscribeAsync(mqttSubscribeOptions);
        _logger.LogInformation("Subscribed to {Count} print request and process result topics with QoS 1 (AtLeastOnce).", topics.Length);
    }

    private async Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs arg)
    {
        _logger.LogWarning("Disconnected from MQTT broker: {Reason}. Auto-reconnect loop will attempt reconnection...", arg.Reason);
        _isConnected = false;
        await SendStatusToHubAsync(false);
    }

    private async Task ConnectMqttAsync(CancellationToken cancellationToken)
    {
        if (_mqttClient == null || _mqttClient.IsConnected)
            return;

        var mqttClientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(_mqttSettings.Broker, _mqttSettings.Port)
            .WithClientId(_mqttSettings.ClientId)
            .WithCleanSession()
            .Build();

        try
        {
            await _mqttClient.ConnectAsync(mqttClientOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to connect to MQTT broker at {Broker}:{Port}: {Message}", _mqttSettings.Broker, _mqttSettings.Port, ex.Message);
        }
    }
}
