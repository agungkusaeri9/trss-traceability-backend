using System.Net.Sockets;
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
        var broker = !string.IsNullOrWhiteSpace(_mqttSettings.Broker) ? _mqttSettings.Broker : "localhost";
        var port = _mqttSettings.Port > 0 ? _mqttSettings.Port : 1883;

        // Perform live TCP reachability check if not marked connected
        var isReachable = await CheckBrokerReachableAsync(broker, port);
        _isConnected = isReachable;

        // Kirim status saat ini ke klien yang baru terhubung
        await Clients.Caller.SendAsync("MqttStatusUpdated", new
        {
            IsConnected = _isConnected,
            Broker = broker,
            Port = port,
            Status = _isConnected ? "Online" : "Offline"
        });

        await base.OnConnectedAsync();
    }

    public async Task GetStatus()
    {
        var broker = !string.IsNullOrWhiteSpace(_mqttSettings.Broker) ? _mqttSettings.Broker : "localhost";
        var port = _mqttSettings.Port > 0 ? _mqttSettings.Port : 1883;

        var isReachable = await CheckBrokerReachableAsync(broker, port);
        _isConnected = isReachable;

        await Clients.Caller.SendAsync("MqttStatusUpdated", new
        {
            IsConnected = _isConnected,
            Broker = broker,
            Port = port,
            Status = _isConnected ? "Online" : "Offline"
        });
    }

    public async Task UpdateStatus(bool isConnected)
    {
        var broker = !string.IsNullOrWhiteSpace(_mqttSettings.Broker) ? _mqttSettings.Broker : "localhost";
        var port = _mqttSettings.Port > 0 ? _mqttSettings.Port : 1883;

        _isConnected = isConnected;
        await Clients.All.SendAsync("MqttStatusUpdated", new
        {
            IsConnected = _isConnected,
            Broker = broker,
            Port = port,
            Status = _isConnected ? "Online" : "Offline"
        });
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }

    private static async Task<bool> CheckBrokerReachableAsync(string host, int port, int timeoutMs = 1500)
    {
        try
        {
            using var client = new TcpClient();
            using var cts = new CancellationTokenSource(timeoutMs);
            await client.ConnectAsync(host, port, cts.Token);
            return client.Connected;
        }
        catch
        {
            return false;
        }
    }
}
