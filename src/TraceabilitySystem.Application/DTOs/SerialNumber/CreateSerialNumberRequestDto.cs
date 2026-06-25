namespace TraceabilitySystem.Application.DTOs.SerialNumber;

public class CreateSerialNumberRequestDto
{
    public string SerialNumberCode { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? CreatedBy { get; set; }
}
