using System;

namespace TraceabilitySystem.Domain.Entities;
public class IssueTransaction
{
    public int Id { get; set; }

    public int IssueId { get; set; }

    public decimal QtyBefore { get; set; }

    public decimal QtyChange  { get; set; }

    public decimal QtyAfter { get; set; }
    public string? Type { get; set; } // "ISSUE" | "RETURN" | "ADJUSTMENT" | "REJECT" 
    public string? Remark { get; set; } 

    public DateTime CreatedAt { get; set; }

     // Navigation
    public virtual Issue Issue { get; set; } = null!;
}