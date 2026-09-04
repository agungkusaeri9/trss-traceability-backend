
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
            await Task.Delay(1000, stoppingToken);
        }

        await _mqttClient.DisconnectAsync(cancellationToken: stoppingToken);
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

            // 1. Deserialisasi JSON secara aman
            CreateProcessLogRequestDto? request = null;
            string? parseError = null;

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
                parseError = $"Invalid JSON payload: {ex.Message}";
                _logger.LogWarning("[MQTT] Failed to deserialize payload for topic [{Topic}]: {Error}", topic, parseError);
            }

            // 2. Selalu simpan pesan mentah MQTT ke database terlebih dahulu
            var initialStatus = parseError == null ? "RECEIVED" : "FAILED";
            try
            {
                await databaseService.SaveMqttMessageAsync(
                    messageId: messageId,
                    topic: topic,
                    payload: payload,
                    operatorUsername: request?.OperatorUsername,
                    isOk: request?.IsOk,
                    status: initialStatus,
                    processName: processName,
                    errorMessage: parseError
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MQTT][Database] Failed to save raw MQTT message to database. Topic: {Topic}", topic);
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

            if (request == null)
            {
                _logger.LogWarning("[MQTT] Request payload deserialized to null for topic [{Topic}]", topic);
                await databaseService.UpdateMqttMessageStatusAsync(messageId, "FAILED", "Deserialized request is null");
                return;
            }

            // 4. Dispatch topic proses ke handler yang sesuai
            Task? processResultTask = topic switch
            {
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

        var mqttSubscribeOptions = new MqttFactory().CreateSubscribeOptionsBuilder()
            // Print request topics
            .WithTopicFilter("traceability/print/request/clinching-short-side")
            .WithTopicFilter("traceability/print/request/m-fan-assy")
            // Process result topics
            .WithTopicFilter("data/process/clinching-short-side/result")
            .WithTopicFilter("data/process/clinching-long-side/result")
            .WithTopicFilter("data/process/he-leak/result")
            .WithTopicFilter("data/process/m-fan-assy/result-scan")
            .WithTopicFilter("data/process/m-fan-assy/result")
            .WithTopicFilter("data/process/m-fan-inspection/result")
            .WithTopicFilter("data/process/ecm-assy/result")
            .WithTopicFilter("data/process/final-inspection/result")
            .Build();

        await _mqttClient!.SubscribeAsync(mqttSubscribeOptions);
        _logger.LogInformation("Subscribed to print request and process result topics.");
    }

    private async Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs arg)
    {
        _logger.LogWarning("Disconnected from MQTT broker. Reconnecting...");
        _isConnected = false;
        await SendStatusToHubAsync(false);

        await Task.Delay(TimeSpan.FromSeconds(5));
        await ConnectMqttAsync(CancellationToken.None);
    }

    private async Task ConnectMqttAsync(CancellationToken cancellationToken)
    {
        var mqttClientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(_mqttSettings.Broker, _mqttSettings.Port)
            .WithClientId(_mqttSettings.ClientId)
            .WithCleanSession()
            .Build();

        try
        {
            await _mqttClient!.ConnectAsync(mqttClientOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Failed to connect to MQTT broker at {Broker}:{Port}: {Message}", _mqttSettings.Broker, _mqttSettings.Port, ex.Message);
        }
    }
}
