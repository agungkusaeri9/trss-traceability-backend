namespace TraceabilitySystem.Application.DTOs.Printer;

public class CreatePrinterRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public int Port { get; set; } = 9100;
    public string? Description { get; set; }
}
