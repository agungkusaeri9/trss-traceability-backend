namespace TraceabilitySystem.Application.Services;

/// <summary>
/// Implementasi no-op dari IMqttPublisher.
/// Digunakan oleh API project yang tidak memiliki koneksi MQTT langsung.
/// Worker project akan meng-override registration ini dengan implementasi nyata (MqttPublisher).
/// </summary>
public class NullMqttPublisher : Interfaces.IMqttPublisher
{
    public Task PublishAsync(string topic, object payload, CancellationToken cancellationToken = default)
    {
        // No-op: API tidak publish MQTT secara langsung; Worker yang menangani.
        return Task.CompletedTask;
    }
}
