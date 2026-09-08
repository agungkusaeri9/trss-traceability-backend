namespace TraceabilitySystem.Application.DTOs.Printer;

public class PrinterStatusDto
{
    public string Key { get; set; } = string.Empty; // "STOCK_IN", "CLINCHING", "MFAN_ASSY"
    public string Name { get; set; } = string.Empty; // "Printer Stock In", "Printer Clinching", "Printer M-Fan Assy"
    public string? PrinterName { get; set; }
    public string? IpAddress { get; set; }
    public int? Port { get; set; }
    public bool IsOnline { get; set; }
    public string Status { get; set; } = "Offline"; // "Online" | "Offline"
    public string? ErrorMessage { get; set; }
    public DateTime LastChecked { get; set; } = DateTime.UtcNow;
}
