namespace TraceabilitySystem.Shared.Models;

public class MqttSettings
{
    public string Broker { get; set; } = "localhost";
    public int Port { get; set; } = 1883;
    public string ClientId { get; set; } = "trss-traceability-backend";
}
