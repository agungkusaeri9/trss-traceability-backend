using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.SignalR.Client;
using MQTTnet;
using MQTTnet.Client;
using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TraceabilitySystem.Application.DTOs.MqttPrintRequest;
using TraceabilitySystem.Application.DTOs.SerialNumber;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Shared.Models;

namespace TraceabilitySystem.Worker.BackgroundServices;

public class MqttPrintRequestService : BackgroundService
{
    private readonly ILogger<MqttPrintRequestService> _logger;
    private readonly MqttSettings _mqttSettings;
    private readonly WorkerSettings _workerSettings;
    private readonly IServiceScopeFactory _scopeFactory;
    
    private IMqttClient? _mqttClient;
    private HubConnection? _hubConnection;
    private bool _isConnected;

    public MqttPrintRequestService(
        ILogger<MqttPrintRequestService> logger,
        IOptions<MqttSettings> mqttSettings,
        IOptions<WorkerSettings> workerSettings,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _mqttSettings = mqttSettings.Value;
        _workerSettings = workerSettings.Value;
        _scopeFactory = scopeFactory;
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

        _logger.LogInformation("[MQTT] Received message on topic [{Topic}]", topic);

        // Dispatch process result topics to dedicated handlers
        var processResultTask = topic switch
        {
            "data/process/clinching-short-side/result" => HandleClinchingShortSideResultAsync(payload),
            "data/process/clinching-long-side/result"  => HandleClinchingLongSideResultAsync(payload),
            "data/process/he-leak/result"              => HandleHeLeakResultAsync(payload),
            "data/process/m-fan-assy/result"           => HandleMFanAssyResultAsync(payload),
            "data/process/m-fan-inspection/result"     => HandleMFanInspectionResultAsync(payload),
            "data/process/ecm-assy/result"             => HandleEcmAssyResultAsync(payload),
            "data/process/final-inspection/result"     => HandleFinalInspectionResultAsync(payload),
            _                                          => null
        };

        if (processResultTask is not null)
        {
            await processResultTask;
            return;
        }

        // Handle print request topics
        using var scope = _scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IMqttPrintRequestService>();

        try
        {
            string processCode = string.Empty;
            if (topic == "traceability/print/request/clinching-short-side")
            {
                processCode = "CLINCHING_SHORT_SIDE";
            }
            else if (topic == "traceability/print/request/m-fan-assy")
            {
                processCode = "M_FAN_ASSY";
            }

            var payloadData = JsonSerializer.Deserialize<JsonElement>(payload);
            string issueNumber = payloadData.TryGetProperty("issue_number", out var ins) ? ins.GetString() ?? string.Empty : string.Empty;

            var savedRequest = await service.CreateAsync(new CreateMqttPrintRequestDto
            {
                ProcessCode = processCode,
                IssueNumber = issueNumber,
                RawPayload = payload
            }, cancellationToken: default);

            _logger.LogInformation("MQTT message saved to DB with ID: {Id}", savedRequest.Id);

            try
            {
                var printService = scope.ServiceProvider.GetRequiredService<IPrintService>();
                if (processCode == "CLINCHING_SHORT_SIDE")
                {
                    // await printService.PrintClinchingShortSideAsync(issueNumber, cancellationToken: default);
                }
                else if (processCode == "M_FAN_ASSY")
                {
                    // await printService.PrintMFanAssyAsync(issueNumber, cancellationToken: default);
                }

                await service.UpdateStatusAsync(savedRequest.Id, "Processed", cancellationToken: default);
                _logger.LogInformation("MQTT print request ID {Id} marked as Processed", savedRequest.Id);
            }
            catch (Exception ex)
            {
                await service.UpdateStatusAsync(savedRequest.Id, "Failed", ex.Message, cancellationToken: default);
                _logger.LogError(ex, "MQTT print request ID {Id} failed", savedRequest.Id);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing MQTT message: {Payload}", payload);
        }
    }

    // -------------------------------------------------------------------------
    // Process Result Handlers (per topic)
    // -------------------------------------------------------------------------

    private async Task HandleClinchingShortSideResultAsync(string payload)
    {
        _logger.LogInformation("[MQTT][ClinchingShortSide] Payload: {Payload}", payload);

        try
        {
            var payloadData = JsonSerializer.Deserialize<JsonElement>(payload);

            List<string>? issueNumbers = null;
            if (payloadData.TryGetProperty("issue_numbers", out var issueNumbersEl) && issueNumbersEl.ValueKind == JsonValueKind.Array)
            {
                issueNumbers = issueNumbersEl.EnumerateArray()
                    .Select(e => e.GetString())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s!)
                    .ToList();
            }

            using var scope = _scopeFactory.CreateScope();
            var serialNumberService = scope.ServiceProvider.GetRequiredService<ISerialNumberService>();

            var request = new GenerateSerialNumberRequestDto
            {
                Type = "CLINCHING",
                Qty = 1,
                CreatedBy = "MQTT_CLINCHING_SHORT_SIDE",
                IssueNumbers = issueNumbers
            };

            _logger.LogInformation("[MQTT][ClinchingShortSide] Generating clinching SN for Issues: {IssueNumbers}", string.Join(", ", issueNumbers ?? new List<string>()));
            var results = await serialNumberService.CreateByClinchingAsync(request);

            foreach (var sn in results)
            {
                _logger.LogInformation("[MQTT][ClinchingShortSide] Generated Clinching SN: {SerialNumber} (Type: {Type})", sn.SerialNumberCode, sn.Type);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MQTT][ClinchingShortSide] Error generating serial number: {Message}", ex.Message);
        }
    }

    private Task HandleClinchingLongSideResultAsync(string payload)
    {
        _logger.LogInformation("[MQTT][ClinchingLongSide] Payload: {Payload}", payload);
        // TODO: implement business logic
        return Task.CompletedTask;
    }

    private Task HandleHeLeakResultAsync(string payload)
    {
        _logger.LogInformation("[MQTT][HeLeak] Payload: {Payload}", payload);
        // TODO: implement business logic
        return Task.CompletedTask;
    }

    private Task HandleMFanAssyResultAsync(string payload)
    {
        _logger.LogInformation("[MQTT][MFanAssy] Payload: {Payload}", payload);
        // TODO: implement business logic
        return Task.CompletedTask;
    }

    private Task HandleMFanInspectionResultAsync(string payload)
    {
        _logger.LogInformation("[MQTT][MFanInspection] Payload: {Payload}", payload);
        // TODO: implement business logic
        return Task.CompletedTask;
    }

    private Task HandleEcmAssyResultAsync(string payload)
    {
        _logger.LogInformation("[MQTT][EcmAssy] Payload: {Payload}", payload);
        // TODO: implement business logic
        return Task.CompletedTask;
    }

    private Task HandleFinalInspectionResultAsync(string payload)
    {
        _logger.LogInformation("[MQTT][FinalInspection] Payload: {Payload}", payload);
        // TODO: implement business logic
        return Task.CompletedTask;
    }

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
