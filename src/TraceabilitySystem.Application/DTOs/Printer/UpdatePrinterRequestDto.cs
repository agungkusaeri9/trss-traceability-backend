namespace TraceabilitySystem.Application.DTOs.Printer;

public class UpdatePrinterRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public int Port { get; set; } = 9100;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}
