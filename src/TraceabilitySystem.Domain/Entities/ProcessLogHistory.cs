using System;

namespace TraceabilitySystem.Domain.Entities;

public class ProcessLogHistory
{
    public long Id { get; set; }

    public long ProcessLogId { get; set; }
    public ProcessLog ProcessLog { get; set; } = null!;

    public int SerialNumberId { get; set; }
    public SerialNumber SerialNumber { get; set; } = null!;

    public int ProcessId { get; set; }
    public Process Process { get; set; } = null!;

    public int ParameterId { get; set; }
    public Parameter Parameter { get; set; } = null!;

    public string? Value { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
