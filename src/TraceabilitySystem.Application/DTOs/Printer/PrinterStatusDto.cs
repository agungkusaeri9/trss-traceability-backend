namespace TraceabilitySystem.Application.DTOs.Printer;

public class PrinterStatusDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public int Port { get; set; }
    public bool IsOnline { get; set; }
    public string Status { get; set; } = string.Empty; // "Online" | "Offline" | "Checking..."
    public DateTime LastChecked { get; set; }
}
