using System;
using System.Collections.Generic;

namespace TraceabilitySystem.Domain.Entities;

public class ProcessLog
{
    public long Id { get; set; }

     public int SerialNumberId { get; set; }
    public SerialNumber SerialNumber { get; set; } = null!;

    public bool IsActive { get; set; }
    public bool Status { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<ProcessLogDetail> Details { get; set; } = new List<ProcessLogDetail>();
}
