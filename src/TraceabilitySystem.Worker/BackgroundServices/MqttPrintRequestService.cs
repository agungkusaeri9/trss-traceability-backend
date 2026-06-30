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
using TraceabilitySystem.Application.DTOs.ProcessLog;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Shared.Models;
using TraceabilitySystem.Worker.Services;

namespace TraceabilitySystem.Worker.BackgroundServices;

public class MqttPrintRequestService : BackgroundService
{
    private readonly ILogger<MqttPrintRequestService> _logger;
    private readonly MqttSettings _mqttSettings;
    private readonly WorkerSettings _workerSettings;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly MqttClientAccessor _mqttClientAccessor;
    
    private IMqttClient? _mqttClient;
    private HubConnection? _hubConnection;
    private bool _isConnected;

    public MqttPrintRequestService(
        ILogger<MqttPrintRequestService> logger,
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

        _logger.LogInformation("[MQTT] Received message on topic [{Topic}]", topic);

        // Dispatch process result topics to dedicated handlers
        var processResultTask = topic switch
        {
            "data/process/clinching-short-side/result" => HandleClinchingShortSideResultAsync(payload),
            "data/process/clinching-long-side/result" => HandleClinchingLongSideResultAsync(payload),
            "data/process/he-leak/result" => HandleHeLeakResultAsync(payload),
            "data/process/m-fan-assy/result-scan" => HandleMFanAssyResultScanAsync(payload),
             "data/process/m-fan-assy/result"           => HandleMFanAssyResultAsync(payload),
            "data/process/m-fan-inspection/result" => HandleMFanInspectionResultAsync(payload),
            "data/process/ecm-assy/result" => HandleEcmAssyResultAsync(payload),
            "data/process/final-inspection/result" => HandleFinalInspectionResultAsync(payload),
            _ => null
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

        using var scope = _scopeFactory.CreateScope();
        var processLogService = scope.ServiceProvider.GetRequiredService<IProcessLogService>();

        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var request = JsonSerializer.Deserialize<CreateProcessLogRequestDto>(payload, options);
            if (request == null)
            {
                _logger.LogWarning("[MQTT][ClinchingShortSide] Failed to deserialize payload");
                return;
            }

            request.ProcessCode = "CLINCHING_SHORT_SIDE";

            _logger.LogInformation("[MQTT][ClinchingShortSide] Calling CreateProcessLogByClinchingAsync...");
            var result = await processLogService.CreateProcessLogByClinchingAsync(request, cancellationToken: default);

            _logger.LogInformation("[MQTT][ClinchingShortSide] Process log created. Id={ProcessLogId}, SN={SerialNumber}", result.Id, result?.SerialNumberCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MQTT][ClinchingShortSide] Error: {Message}", ex.Message);
        }
    }

    private async Task HandleClinchingLongSideResultAsync(string payload)
    {
        _logger.LogInformation("[MQTT][ClinchingLongSide] Payload: {Payload}", payload);
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var request = JsonSerializer.Deserialize<CreateProcessLogRequestDto>(payload, options);
            if (request == null)
            {
                _logger.LogWarning("[MQTT][ClinchingLongSide] Failed to deserialize payload");
                return;
            }

            request.ProcessCode = "CLINCHING_LONG_SIDE";

            using var scope = _scopeFactory.CreateScope();
            var processLogService = scope.ServiceProvider.GetRequiredService<IProcessLogService>();
            
            var result = await processLogService.CreateProcessLogDetailOnlyAsync(request);
            _logger.LogInformation("[MQTT][ClinchingLongSide] Successfully created process log details with ID: {ProcessLogId} for SN: {SerialNumber}", result.Id, request.SerialNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MQTT][ClinchingLongSide] Error handling process result: {Message}", ex.Message);
        }
    }

    private async Task HandleHeLeakResultAsync(string payload)
    {
        _logger.LogInformation("[MQTT][HeLeak] Payload: {Payload}", payload);
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var request = JsonSerializer.Deserialize<CreateProcessLogRequestDto>(payload, options);
            if (request == null)
            {
                _logger.LogWarning("[MQTT][HeLeak] Failed to deserialize payload");
                return;
            }

            request.ProcessCode = "HE_LEAK";

            using var scope = _scopeFactory.CreateScope();
            var processLogService = scope.ServiceProvider.GetRequiredService<IProcessLogService>();
            
            var result = await processLogService.CreateProcessLogDetailOnlyAsync(request);
            _logger.LogInformation("[MQTT][HeLeak] Successfully created process log details with ID: {ProcessLogId} for SN: {SerialNumber}", result.Id, request.SerialNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MQTT][HeLeak] Error handling process result: {Message}", ex.Message);
        }
    }

    private async Task HandleMFanAssyResultScanAsync(string payload)
    {
        _logger.LogInformation("[MQTT][MFanAssyScan] Payload: {Payload}", payload);
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var request = JsonSerializer.Deserialize<CreateProcessLogRequestDto>(payload, options);
            if (request == null)
            {
                _logger.LogWarning("[MQTT][MFanAssyScan] Failed to deserialize payload");
                return;
            }

            request.ProcessCode = "M_FAN_ASSY"; 
            request.SerialNumber = request.SerialNumberClinching;

            using var scope = _scopeFactory.CreateScope();
            var processLogService = scope.ServiceProvider.GetRequiredService<IProcessLogService>();

            var result = await processLogService.CreateProcessLogMFanAssyAsync(request,"create_with_issue_number");
            _logger.LogInformation("[MQTT][MFanAssyScan] Successfully created process log details with ID: {ProcessLogId} for SN: {SerialNumber}", result.Id, request.SerialNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MQTT][MFanAssy] Error handling process result: {Message}", ex.Message);
        }
    }
    
     private async Task HandleMFanAssyResultAsync(string payload)
    {
        _logger.LogInformation("[MQTT][MFanAssy] Payload: {Payload}", payload);
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var request = JsonSerializer.Deserialize<CreateProcessLogRequestDto>(payload, options);
            if (request == null)
            {
                _logger.LogWarning("[MQTT][MFanAssy] Failed to deserialize payload");
                return;
            }

            request.ProcessCode = "M_FAN_ASSY";
            request.SerialNumber = request.SerialNumberMFanAssy;

            using var scope = _scopeFactory.CreateScope();
            var processLogService = scope.ServiceProvider.GetRequiredService<IProcessLogService>();
            
            var result = await processLogService.CreateProcessLogMFanAssyAsync(request, "create_without_issue_number");
            _logger.LogInformation("[MQTT][MFanAssy] Successfully created process log details with ID: {ProcessLogId} for SN: {SerialNumber}", result.Id, request.SerialNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MQTT][MFanAssy] Error handling process result: {Message}", ex.Message);
        }
    }

    private async Task HandleMFanInspectionResultAsync(string payload)
    {
        _logger.LogInformation("[MQTT][MFanInspection] Payload: {Payload}", payload);
         try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var request = JsonSerializer.Deserialize<CreateProcessLogRequestDto>(payload, options);
            if (request == null)
            {
                _logger.LogWarning("[MQTT][MFanInspection] Failed to deserialize payload");
                return;
            }

            request.ProcessCode = "M_FAN_INSPECTION";
            request.SerialNumber = request.SerialNumberMFanAssy;

            using var scope = _scopeFactory.CreateScope();
            var processLogService = scope.ServiceProvider.GetRequiredService<IProcessLogService>();
            
            var result = await processLogService.CreateProcessLogDetailOnlyAsync(request);
            _logger.LogInformation("[MQTT][MFanInspection] Successfully created process log details with ID: {ProcessLogId} for SN: {SerialNumber}", result.Id, request.SerialNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MQTT][MFanInspection] Error handling process result: {Message}", ex.Message);
        }
    }

    private async Task HandleEcmAssyResultAsync(string payload)
    {
        _logger.LogInformation("[MQTT][EcmAssy] Payload: {Payload}", payload);
         try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var request = JsonSerializer.Deserialize<CreateProcessLogRequestDto>(payload, options);
            if (request == null)
            {
                _logger.LogWarning("[MQTT][EcmAssy] Failed to deserialize payload");
                return;
            }

            request.ProcessCode = "ECM_ASSY";

            using var scope = _scopeFactory.CreateScope();
            var processLogService = scope.ServiceProvider.GetRequiredService<IProcessLogService>();
            
            var result = await processLogService.CreateProcessLogDetailOnlyAsync(request);
            _logger.LogInformation("[MQTT][EcmAssy] Successfully created process log details with ID: {ProcessLogId} for SN: {SerialNumber}", result.Id, request.SerialNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MQTT][EcmAssy] Error handling process result: {Message}", ex.Message);
        }
    }

    private async Task HandleFinalInspectionResultAsync(string payload)
    {
        _logger.LogInformation("[MQTT][FinalInspection] Payload: {Payload}", payload);
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var request = JsonSerializer.Deserialize<CreateProcessLogRequestDto>(payload, options);
            if (request == null)
            {
                _logger.LogWarning("[MQTT][FinalInspection] Failed to deserialize payload");
                return;
            }

            request.ProcessCode = "FINAL_INSPECTION";

            using var scope = _scopeFactory.CreateScope();
            var processLogService = scope.ServiceProvider.GetRequiredService<IProcessLogService>();
            
            var result = await processLogService.CreateProcessLogDetailOnlyAsync(request);
            _logger.LogInformation("[MQTT][FinalInspection] Successfully created process log details with ID: {ProcessLogId} for SN: {SerialNumber}", result.Id, request.SerialNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MQTT][FinalInspection] Error handling process result: {Message}", ex.Message);
        }
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
