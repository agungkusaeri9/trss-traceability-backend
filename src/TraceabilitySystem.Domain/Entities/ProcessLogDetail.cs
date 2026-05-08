using System;

namespace TraceabilitySystem.Domain.Entities;

public class ProcessLogDetail
{
    public long Id { get; set; }

    public long ProcessLogId { get; set; }
    public ProcessLog? ProcessLog { get; set; }

    public int ProcessId { get; set; }
    public Process? Process { get; set; }

    public int ParameterId { get; set; }
    public Parameter? Parameter { get; set; }

    public decimal? ValueNumber { get; set; }
    public string? ValueText { get; set; }
    public bool? ValueBoolean { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
}
