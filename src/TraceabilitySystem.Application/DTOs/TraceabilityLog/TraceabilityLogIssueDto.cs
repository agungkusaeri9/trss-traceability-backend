namespace TraceabilitySystem.Application.DTOs.TraceabilityLog;

public class TraceabilityLogIssueDto
{
    public string SerialNumber { get; set; } = string.Empty;
    public List<TraceabilityLogIssueItemDto> Issues { get; set; } = new();
}

public class TraceabilityLogIssueItemDto
{
    public string IssueNumber { get; set; } = string.Empty;
    public string? PartNumber { get; set; }
    public string? PartName { get; set; }
}
