using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using System.Text;
using System.Text.Json;
using TraceabilitySystem.API.Hubs;
using TraceabilitySystem.Application.DTOs.MqttPrintRequest;
using TraceabilitySystem.Application.Interfaces;

namespace TraceabilitySystem.API.BackgroundServices;

public class MqttPrintRequestService : BackgroundService
{
    private readonly ILogger<MqttPrintRequestService> _logger;
    private readonly MqttSettings _mqttSettings;
    private readonly IHubContext<MqttStatusHub> _hubContext;
    private readonly IServiceScopeFactory _scopeFactory;
    private IMqttClient? _mqttClient;
    private static bool _isConnected;

    public static bool IsConnected => _isConnected;

    public MqttPrintRequestService(
        ILogger<MqttPrintRequestService> logger,
        IOptions<MqttSettings> mqttSettings,
        IHubContext<MqttStatusHub> hubContext,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _mqttSettings = mqttSettings.Value;
        _hubContext = hubContext;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MqttPrintRequestService started.");

        var factory = new MqttFactory();
        _mqttClient = factory.CreateMqttClient();

        _mqttClient.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;
        _mqttClient.ConnectedAsync += OnConnectedAsync;
        _mqttClient.DisconnectedAsync += OnDisconnectedAsync;

        await ConnectAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }

        await _mqttClient.DisconnectAsync(cancellationToken: stoppingToken);
    }

    private async Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs arg)
    {
        var topic = arg.ApplicationMessage.Topic;
        var payload = Encoding.UTF8.GetString(arg.ApplicationMessage.PayloadSegment);

        _logger.LogInformation("Received MQTT message on topic {Topic}: {Payload}", topic, payload);

        using var scope = _scopeFactory.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IMqttPrintRequestService>();

        try
        {
            // Tentukan processCode berdasarkan topic
            string processCode = string.Empty;
            if (topic == "traceability/print/request/clinching-short-side")
            {
                processCode = "CLINCHING_SHORT_SIDE";
            }
            else if (topic == "traceability/print/request/m-fan-assy")
            {
                processCode = "M_FAN_ASSY";
            }

            // Parse payload untuk mendapatkan issue_number
            var payloadData = JsonSerializer.Deserialize<JsonElement>(payload);
            string issueNumber = payloadData.TryGetProperty("issue_number", out var ins) ? ins.GetString() ?? string.Empty : string.Empty;

            // Simpan via service
            var savedRequest = await service.CreateAsync(new CreateMqttPrintRequestDto
            {
                ProcessCode = processCode,
                IssueNumber = issueNumber,
                RawPayload = payload
            }, cancellationToken: default);

            _logger.LogInformation("MQTT message saved to DB with ID: {Id}", savedRequest.Id);

            try
            {
                // Lanjutkan proses print di sini
                var printService = scope.ServiceProvider.GetRequiredService<IPrintService>();
                if (processCode == "CLINCHING_SHORT_SIDE")
                {
                    // await printService.PrintClinchingShortSideAsync(issueNumber, cancellationToken: default);
                }
                else if (processCode == "M_FAN_ASSY")
                {
                    // await printService.PrintMFanAssyAsync(issueNumber, cancellationToken: default);
                }

                // Update status menjadi Processed
                await service.UpdateStatusAsync(savedRequest.Id, "Processed", cancellationToken: default);
                _logger.LogInformation("MQTT print request ID {Id} marked as Processed", savedRequest.Id);
            }
            catch (Exception ex)
            {
                // Update status menjadi Failed
                await service.UpdateStatusAsync(savedRequest.Id, "Failed", ex.Message, cancellationToken: default);
                _logger.LogError(ex, "MQTT print request ID {Id} failed", savedRequest.Id);
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing MQTT message: {Payload}", payload);

            // Simpan error juga via service (kalau mau) atau tetap simpan manual
            try
            {
                // Atau bisa tambahkan method CreateFailed di service kalo mau
                var failedRequest = new CreateMqttPrintRequestDto
                {
                    ProcessCode = string.Empty,
                    IssueNumber = string.Empty,
                    RawPayload = payload
                };
                // Untuk status failed, mungkin perlu method tersendiri, tapi untuk sekarang kita bisa simpan manual via repository jika perlu
                // tapi untuk sekarang kita log saja
            }
            catch (Exception dbEx)
            {
                _logger.LogError(dbEx, "Error saving failed MQTT request to DB");
            }
        }
    }

    private async Task OnConnectedAsync(MqttClientConnectedEventArgs arg)
    {
        _logger.LogInformation("Connected to MQTT broker.");
        _isConnected = true;
        await BroadcastMqttStatusAsync();

        var mqttSubscribeOptions = new MqttFactory().CreateSubscribeOptionsBuilder()
            .WithTopicFilter("traceability/print/request/clinching-short-side")
            .WithTopicFilter("traceability/print/request/m-fan-assy")
            .Build();

        await _mqttClient!.SubscribeAsync(mqttSubscribeOptions);
        _logger.LogInformation("Subscribed to topics: traceability/print/request/clinching-short-side, traceability/print/request/m-fan-assy");
    }

    private async Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs arg)
    {
        _logger.LogWarning("Disconnected from MQTT broker. Reconnecting...");
        _isConnected = false;
        await BroadcastMqttStatusAsync();

        await Task.Delay(TimeSpan.FromSeconds(5));
        await ConnectAsync(CancellationToken.None);
    }

    private async Task ConnectAsync(CancellationToken cancellationToken)
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

    private async Task BroadcastMqttStatusAsync()
    {
        var status = _isConnected ? "Online" : "Offline";
        _logger.LogInformation("MQTT connection status: {Status} (Broker: {Broker}:{Port})", status, _mqttSettings.Broker, _mqttSettings.Port);

        await _hubContext.Clients.All.SendAsync("MqttStatusUpdated", new
        {
            IsConnected = _isConnected,
            Broker = _mqttSettings.Broker,
            Port = _mqttSettings.Port,
            Status = status
        });
    }
}

public class MqttSettings
{
    public string Broker { get; set; } = "localhost";
    public int Port { get; set; } = 1883;
    public string ClientId { get; set; } = "trss-traceability-backend";
}
