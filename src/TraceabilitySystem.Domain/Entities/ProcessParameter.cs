using System;

namespace TraceabilitySystem.Domain.Entities;

public class ProcessParameter
{
    public int ProcessId { get; set; }
    public Process? Process { get; set; }

    public int ParameterId { get; set; }
    public Parameter? Parameter { get; set; }
}
