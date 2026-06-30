using System;

namespace TraceabilitySystem.Domain.Entities;

public class Parameter
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string DataType { get; set; } = string.Empty; // boolean, text, number
    public int? Order { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<ProcessParameter> ProcessParameters { get; set; } = new List<ProcessParameter>();
}
