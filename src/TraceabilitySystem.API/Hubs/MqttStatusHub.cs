using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using TraceabilitySystem.API.BackgroundServices;

namespace TraceabilitySystem.API.Hubs;

/// <summary>
/// SignalR Hub for real-time MQTT connection status monitoring.
/// Clients connect here to receive live MQTT connection status updates.
/// </summary>
public class MqttStatusHub : Hub
{
    private readonly MqttSettings _mqttSettings;

    public MqttStatusHub(IOptions<MqttSettings> mqttSettings)
    {
        _mqttSettings = mqttSettings.Value;
    }

    public override async Task OnConnectedAsync()
    {
        // Kirim status saat ini ke klien yang baru terhubung
        await Clients.Caller.SendAsync("MqttStatusUpdated", new
        {
            IsConnected = MqttPrintRequestService.IsConnected,
            Broker = _mqttSettings.Broker,
            Port = _mqttSettings.Port,
            Status = MqttPrintRequestService.IsConnected ? "Online" : "Offline"
        });

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}
