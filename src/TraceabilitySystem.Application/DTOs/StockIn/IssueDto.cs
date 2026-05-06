using System;

namespace TraceabilitySystem.Application.DTOs.StockIn;

public class IssueDto
{
    public int Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public int StockInId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
