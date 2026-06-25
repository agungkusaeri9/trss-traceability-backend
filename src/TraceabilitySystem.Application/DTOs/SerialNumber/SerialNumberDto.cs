using System;

namespace TraceabilitySystem.Application.DTOs.SerialNumber;

public class SerialNumberDto
{
    public int Id { get; set; }
    public string SerialNumberCode { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
