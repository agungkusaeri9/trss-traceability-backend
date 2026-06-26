using MQTTnet.Client;

namespace TraceabilitySystem.Worker.Services;

/// <summary>
/// Singleton holder untuk menyimpan referensi IMqttClient yang aktif.
/// Di-set oleh MqttPrintRequestService setelah koneksi berhasil.
/// </summary>
public class MqttClientAccessor
{
    private IMqttClient? _client;

    public IMqttClient? Client
    {
        get => _client;
        set => _client = value;
    }

    public bool IsConnected => _client?.IsConnected ?? false;
}
