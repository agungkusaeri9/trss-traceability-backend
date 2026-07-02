
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.SignalR.Client;
using MQTTnet;
using MQTTnet.Client;
using System;
using System.Text;
using System.Text.Json;
using TraceabilitySystem.Application.DTOs.ProcessLog;
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
    private readonly DatabaseService _databaseService;

    private readonly MqttSubscriptionService _mqttSubsriptionService;

    private IMqttClient? _mqttClient;
    private HubConnection? _hubConnection;
    private bool _isConnected;

    public MqttWorkerService(
        ILogger<MqttWorkerService> logger,
        IOptions<MqttSettings> mqttSettings,
        IOptions<WorkerSettings> workerSettings,
        IServiceScopeFactory scopeFactory,
        MqttClientAccessor mqttClientAccessor,
        DatabaseService databaseService,
        MqttSubscriptionService mqttSubscriptionService)
    {
        _logger = logger;
        _mqttSettings = mqttSettings.Value;
        _workerSettings = workerSettings.Value;
        _scopeFactory = scopeFactory;
        _mqttClientAccessor = mqttClientAccessor;
        _databaseService = databaseService;
        _mqttSubsriptionService = mqttSubscriptionService;
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
            _logger.LogError(ex, "Failed to connect to API SignalR Hub on startup. Automatic reconnect will handle subsequent attempts.");
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
            _logger.LogError(ex, "Failed to send connection status to API Hub");
        }
    }

    private async Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg)
    {
        var topic = arg.ApplicationMessage.Topic;
        var payload = Encoding.UTF8.GetString(arg.ApplicationMessage.PayloadSegment);

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var request = JsonSerializer.Deserialize<CreateProcessLogRequestDto>(payload, options);


        _logger.LogInformation("[MQTT] Received message on topic [{Topic}]", topic);

        // Dispatch process result topics to dedicated handlers
        var processResultTask = topic switch
        {
            "data/process/clinching-short-side/result" =>   _mqttSubsriptionService.HandleClinchingShortSideResultAsync(payload, request),
            "data/process/clinching-long-side/result" =>  _mqttSubsriptionService.HandleClinchingLongSideResultAsync(payload, request),
            "data/process/he-leak/result" =>  _mqttSubsriptionService.HandleHeLeakResultAsync(payload, request),
            "data/process/m-fan-assy/result-scan" =>  _mqttSubsriptionService.HandleMFanAssyResultScanAsync(payload, request),
             "data/process/m-fan-assy/result"           =>  _mqttSubsriptionService.HandleMFanAssyResultAsync(payload, request),
            "data/process/m-fan-inspection/result" =>  _mqttSubsriptionService.HandleMFanInspectionResultAsync(payload, request),
            "data/process/ecm-assy/result" =>  _mqttSubsriptionService.HandleEcmAssyResultAsync(payload, request),
            "data/process/final-inspection/result" =>  _mqttSubsriptionService.HandleFinalInspectionResultAsync(payload, request),
            _ => null
        };

        if (processResultTask is not null)
        {
            await processResultTask;
            return;
        }
    }

    // -------------------------------------------------------------------------
    // Process Result Handlers (per topic)
    // -------------------------------------------------------------------------

   

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
            _logger.LogError(ex, "Failed to connect to MQTT broker at {Broker}:{Port}", _mqttSettings.Broker, _mqttSettings.Port);
        }
    }
}
