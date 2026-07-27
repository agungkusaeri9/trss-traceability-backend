namespace TraceabilitySystem.Application.DTOs.ProcessLog;

public class ExportProcessLogDto
{
    public string SerialNumberCode { get; set; } = string.Empty;
    public string? IssueNumber1 { get; set; }
    public string? IssueNumber2 { get; set; }
    public string? IssueNumber3 { get; set; }
    public string? IssueNumber4 { get; set; }
    public string? IssueNumber5 { get; set; }
    public string? IssueNumber6 { get; set; }
    public string ProcessName { get; set; } = string.Empty;
    public string ParameterName { get; set; } = string.Empty;
    public string? Value { get; set; }
    public bool Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
