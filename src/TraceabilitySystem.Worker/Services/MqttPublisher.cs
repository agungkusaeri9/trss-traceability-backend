using System.Text;
using System.Text.Json;
using MQTTnet;
using MQTTnet.Client;
using TraceabilitySystem.Application.Interfaces;

namespace TraceabilitySystem.Worker.Services;

/// <summary>
/// Implementasi IMqttPublisher menggunakan MQTTnet.
/// Menggunakan MqttClientAccessor untuk mendapatkan IMqttClient yang aktif.
/// </summary>
public class MqttPublisher : IMqttPublisher
{
    private readonly MqttClientAccessor _mqttClientAccessor;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = false
    };

    public MqttPublisher(MqttClientAccessor mqttClientAccessor)
    {
        _mqttClientAccessor = mqttClientAccessor;
    }

    public async Task PublishAsync(string topic, object payload, CancellationToken cancellationToken = default)
    {
        var client = _mqttClientAccessor.Client;
        if (client == null || !client.IsConnected)
        {
            // MQTT client belum terkoneksi, skip publish
            return;
        }

        var json = JsonSerializer.Serialize(payload, _jsonOptions);
        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(Encoding.UTF8.GetBytes(json))
            .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
            .Build();

        await client.PublishAsync(message, cancellationToken);
    }
}
