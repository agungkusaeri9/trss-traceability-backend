using System;
using System.Collections.Generic;

namespace TraceabilitySystem.Domain.Entities;

public class SerialNumber
{
    public int Id { get; set; }

    public string SerialNumberCode { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty; // CLINCHING or MFanAssy

    public DateTime CreatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? UpdatedBy { get; set; }

    public virtual ICollection<SerialNumberIssue> Issues { get; set; } = new List<SerialNumberIssue>();

    public virtual ICollection<SerialNumberRelation> ParentRelations { get; set; } = new List<SerialNumberRelation>();

    public virtual ICollection<SerialNumberRelation> ChildRelations { get; set; } = new List<SerialNumberRelation>();
     public virtual ICollection<ProcessLog> ProcessLogs { get; set; }
        = new List<ProcessLog>();
}