using System.Text;
using System.Text.Json;
using MQTTnet;
using MQTTnet.Client;
using TraceabilitySystem.Application.Interfaces;

using Microsoft.Extensions.Logging;

namespace TraceabilitySystem.Worker.Services;

/// <summary>
/// Implementasi IMqttPublisher menggunakan MQTTnet.
/// Menggunakan MqttClientAccessor untuk mendapatkan IMqttClient yang aktif.
/// </summary>
public class MqttPublisher : IMqttPublisher
{
    private readonly MqttClientAccessor _mqttClientAccessor;
    private readonly ILogger<MqttPublisher> _logger;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = false
    };

    public MqttPublisher(
        MqttClientAccessor mqttClientAccessor,
        ILogger<MqttPublisher> logger)
    {
        _mqttClientAccessor = mqttClientAccessor;
        _logger = logger;
    }

    public async Task PublishAsync(string topic, object payload, CancellationToken cancellationToken = default)
    {
        string json = string.Empty;
        try
        {
            json = JsonSerializer.Serialize(payload, _jsonOptions);
            var client = _mqttClientAccessor.Client;
            if (client == null || !client.IsConnected)
            {
                _logger.LogWarning("[MQTT] Gagal publish ke topic [{Topic}] karena MQTT client belum terkoneksi.", topic);
                return;
            }

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(Encoding.UTF8.GetBytes(json))
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

            await client.PublishAsync(message, cancellationToken);
            _logger.LogInformation("[MQTT] Berhasil publish ke topic [{Topic}]: {Payload}", topic, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MQTT] Gagal publish ke topic [{Topic}]: {Payload}", topic, json);
        }
    }
}
