using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Shared.Models;

namespace TraceabilitySystem.Infrastructure.Services;

/// <summary>
/// Implementasi IMqttPublisher untuk API / Infrastructure menggunakan MQTTnet.
/// Mengelola koneksi MQTT client secara mandiri dengan auto-connect dan logging terperinci.
/// </summary>
public class MqttPublisher : IMqttPublisher, IAsyncDisposable
{
    private readonly ILogger<MqttPublisher> _logger;
    private readonly MqttSettings _mqttSettings;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private IMqttClient? _mqttClient;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = false
    };

    public MqttPublisher(
        ILogger<MqttPublisher> logger,
        IOptions<MqttSettings> mqttSettings)
    {
        _logger = logger;
        _mqttSettings = mqttSettings.Value;
    }

    private async Task<IMqttClient?> GetConnectedClientAsync(CancellationToken cancellationToken)
    {
        if (_mqttClient != null && _mqttClient.IsConnected)
            return _mqttClient;

        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            if (_mqttClient != null && _mqttClient.IsConnected)
                return _mqttClient;

            if (string.IsNullOrEmpty(_mqttSettings.Broker))
            {
                _logger.LogWarning("[MQTT] Broker address belum dikonfigurasi. Publish dibatalkan.");
                return null;
            }

            var factory = new MqttFactory();
            _mqttClient ??= factory.CreateMqttClient();

            if (!_mqttClient.IsConnected)
            {
                var clientId = string.IsNullOrWhiteSpace(_mqttSettings.ClientId)
                    ? $"trss-api-pub-{Guid.NewGuid():N}"
                    : $"{_mqttSettings.ClientId}-pub-{Guid.NewGuid().ToString("N")[..6]}";

                var port = _mqttSettings.Port > 0 ? _mqttSettings.Port : 1883;

                var options = new MqttClientOptionsBuilder()
                    .WithTcpServer(_mqttSettings.Broker, port)
                    .WithClientId(clientId)
                    .WithCleanSession()
                    .Build();

                _logger.LogInformation("[MQTT] Menghubungkan ke broker MQTT {Broker}:{Port} (ClientId: {ClientId})...", _mqttSettings.Broker, port, clientId);
                await _mqttClient.ConnectAsync(options, cancellationToken);
                _logger.LogInformation("[MQTT] Berhasil terhubung ke broker MQTT.");
            }

            return _mqttClient;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[MQTT] Gagal terhubung ke broker MQTT di {Broker}:{Port}", _mqttSettings.Broker, _mqttSettings.Port);
            return null;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public async Task PublishAsync(string topic, object payload, CancellationToken cancellationToken = default)
    {
        string json = string.Empty;
        try
        {
            json = JsonSerializer.Serialize(payload, _jsonOptions);
            var client = await GetConnectedClientAsync(cancellationToken);
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

    public async ValueTask DisposeAsync()
    {
        if (_mqttClient != null)
        {
            try
            {
                if (_mqttClient.IsConnected)
                {
                    await _mqttClient.DisconnectAsync();
                }
            }
            catch
            {
                // Ignore disconnect exceptions on disposal
            }
            _mqttClient.Dispose();
        }
        _connectionLock.Dispose();
    }
}
