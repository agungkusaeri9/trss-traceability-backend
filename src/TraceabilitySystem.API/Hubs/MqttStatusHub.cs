using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using TraceabilitySystem.Shared.Models;

namespace TraceabilitySystem.API.Hubs;

/// <summary>
/// SignalR Hub for real-time MQTT connection status monitoring.
/// Clients connect here to receive live MQTT connection status updates.
/// </summary>
public class MqttStatusHub : Hub
{
    private readonly MqttSettings _mqttSettings;
    private static bool _isConnected;

    public static bool IsConnected => _isConnected;

    public MqttStatusHub(IOptions<MqttSettings> mqttSettings)
    {
        _mqttSettings = mqttSettings.Value;
    }

    public override async Task OnConnectedAsync()
    {
        // Kirim status saat ini ke klien yang baru terhubung
        await Clients.Caller.SendAsync("MqttStatusUpdated", new
        {
            IsConnected = _isConnected,
            Broker = _mqttSettings.Broker,
            Port = _mqttSettings.Port,
            Status = _isConnected ? "Online" : "Offline"
        });

        await base.OnConnectedAsync();
    }

    public async Task UpdateStatus(bool isConnected)
    {
        _isConnected = isConnected;
        await Clients.All.SendAsync("MqttStatusUpdated", new
        {
            IsConnected = _isConnected,
            Broker = _mqttSettings.Broker,
            Port = _mqttSettings.Port,
            Status = _isConnected ? "Online" : "Offline"
        });
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}
