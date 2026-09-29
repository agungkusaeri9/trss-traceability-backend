namespace TraceabilitySystem.Application.DTOs.Issue;

public class IssueTransactionDto
{
    public int Id { get; set; }
    public int IssueId { get; set; }
    public string? IssueNumber { get; set; }
    public int QtyBefore { get; set; }
    public int QtyChange { get; set; }
    public int QtyAfter { get; set; }
    public string? Type { get; set; }
    public string? Remark { get; set; }
    public DateTime CreatedAt { get; set; }
}
