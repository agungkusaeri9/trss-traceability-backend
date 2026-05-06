namespace TraceabilitySystem.Application.DTOs.Part;

public class UpdatePartRequestDto
{
    public string Number { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; } = string.Empty;
    public bool? IsActive { get; set; }
}