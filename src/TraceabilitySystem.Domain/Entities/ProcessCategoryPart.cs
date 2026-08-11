namespace TraceabilitySystem.Domain.Entities;

public class ProcessCategoryPart
{
    public int Id { get; set; }
    public int ProcessCategoryId { get; set; }
    public int PartId { get; set; }

    // Navigation
    public virtual ProcessCategory? ProcessCategory { get; set; }
    public virtual Part? Part { get; set; }
}
