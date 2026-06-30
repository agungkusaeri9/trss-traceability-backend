using System;

namespace TraceabilitySystem.Domain.Entities;

public class StockInRework
{
    public long Id { get; set; }

    public int SerialNumberId { get; set; }
    public SerialNumber? SerialNumber { get; set; }

    public string IssueNumberBefore { get; set; } = string.Empty;
    public string IssueNumberAfter { get; set; } = string.Empty;

    public int Qty { get; set; }
    public string? Note { get; set; }

    /// <summary>Status rework: true = OK, false = NG</summary>
    public bool Status { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
