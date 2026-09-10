using System;
using System.Collections.Generic;

namespace TraceabilitySystem.Domain.Entities;

public class TraceabilityLog
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? SerialNumberClinching { get; set; }
    public string? SerialNumberMFan { get; set; }
    public bool Status { get; set; } = true;
    public bool IsFinish { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }


    // Navigation
    public ICollection<TraceabilityLogDetail> Details { get; set; } = new List<TraceabilityLogDetail>();
}
