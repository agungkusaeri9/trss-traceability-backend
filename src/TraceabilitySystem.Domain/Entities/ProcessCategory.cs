namespace TraceabilitySystem.Domain.Entities;

public class ProcessCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public virtual ICollection<ProcessCategoryPart> ProcessCategoryParts { get; set; }
        = new List<ProcessCategoryPart>();
}
