using System;

namespace TraceabilitySystem.Domain.Entities;
public class IssueTransaction
{
    public int Id { get; set; }

    public int IssueId { get; set; }

    public int QtyBefore { get; set; }
    public int QtyChange { get; set; }
    public int QtyAfter { get; set; }
    public string? Type { get; set; } // "ISSUE" | "RETURN" | "ADJUSTMENT" | "REJECT" 
    public string? Remark { get; set; } 

    public DateTime CreatedAt { get; set; }

     // Navigation
    public virtual Issue Issue { get; set; } = null!;
}