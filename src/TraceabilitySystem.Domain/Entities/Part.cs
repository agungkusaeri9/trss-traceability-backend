namespace TraceabilitySystem.Domain.Entities;

public class Part
{
    public int Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; } = string.Empty;
    public string? SpecialCharacter { get; set; } = string.Empty;
    public string? CategoryProcess { get; set; } = string.Empty;
    public int QtyStandart { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}