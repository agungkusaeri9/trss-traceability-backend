namespace TraceabilitySystem.Application.DTOs.AppConfig;

public class UpdateAppConfigRequestDto
{
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
}
