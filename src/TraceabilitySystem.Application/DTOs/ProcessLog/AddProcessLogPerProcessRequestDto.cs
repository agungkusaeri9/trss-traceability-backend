namespace TraceabilitySystem.Application.DTOs.ProcessLog;

public class AddProcessLogPerProcessRequestDto
{
    public string SerialNumberCode { get; set; } = string.Empty;
    public string ProcessCode { get; set; } = string.Empty;
    public bool IsOk { get; set; } = true;
    public List<ProcessLogParameterDto> Parameters { get; set; } = new();
}

public class ProcessLogParameterDto
{
    public string ParameterCode { get; set; } = string.Empty;
    public decimal? ValueNumber { get; set; }
    public string? ValueText { get; set; }
    public bool? ValueBoolean { get; set; }
}
