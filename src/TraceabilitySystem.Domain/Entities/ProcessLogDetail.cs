using System;

namespace TraceabilitySystem.Domain.Entities;

public class ProcessLogDetail
{
    public long Id { get; set; }

    public long ProcessLogId { get; set; }
    public ProcessLog ProcessLog { get; set; } = null!;

    public int ProcessId { get; set; }
    public Process Process { get; set; } = null!;

    public int ParameterId { get; set; }
    public Parameter Parameter { get; set; } = null!;

    public decimal? ValueNumber { get; set; }

    public string? ValueText { get; set; }

    public bool? ValueBoolean { get; set; }
    public string? DisplayValue => ValueNumber?.ToString() ?? ValueText ?? ValueBoolean?.ToString() ?? string.Empty;
    public bool Status { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
}
