using System;

namespace TraceabilitySystem.Domain.Entities;

public class TraceabilityLogDetail
{
    public long Id { get; set; }

    public long TraceabilityLogId { get; set; }
    public TraceabilityLog TraceabilityLog { get; set; } = null!;

    public int ProcessId { get; set; }
    public Process Process { get; set; } = null!;

    public int ParameterId { get; set; }
    public Parameter Parameter { get; set; } = null!;

    public string? Value { get; set; }



    public bool Status { get; set; } = true;
    public bool IsFinish { get; set; } = false;

    public int? OperatorId { get; set; }
    public User? Operator { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
