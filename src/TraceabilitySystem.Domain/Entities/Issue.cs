using System;

namespace TraceabilitySystem.Domain.Entities;

public class Issue
{
    public int Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public int StockInId { get; set; }
    public StockIn? StockIn { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public virtual ICollection<SerialNumberIssue> SerialNumberIssues { get; set; }
    = new List<SerialNumberIssue>();

    public virtual ICollection<IssueTransaction> Transactions { get; set; }
        = new List<IssueTransaction>();
}
