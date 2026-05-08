using System;
using System.Collections.Generic;

namespace TraceabilitySystem.Domain.Entities;

public class ProcessLog
{
    public long Id { get; set; }

    public string IssueNo { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<ProcessLogDetail> Details { get; set; } = new List<ProcessLogDetail>();
}
