namespace TraceabilitySystem.Application.Interfaces;

/// <summary>
/// Abstraksi untuk publish pesan ke MQTT broker.
/// Diimplementasikan di Worker layer menggunakan MQTTnet.
/// </summary>
public interface IMqttPublisher
{
    /// <summary>
    /// Publish pesan JSON ke topic tertentu.
    /// </summary>
    /// <param name="topic">MQTT topic tujuan.</param>
    /// <param name="payload">Objek yang akan di-serialize menjadi JSON.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PublishAsync(string topic, object payload, CancellationToken cancellationToken = default);
}
