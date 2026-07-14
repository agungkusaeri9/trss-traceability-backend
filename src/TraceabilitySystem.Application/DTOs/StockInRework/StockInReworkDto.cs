using System;

namespace TraceabilitySystem.Application.DTOs.StockInRework;

public class StockInReworkDto
{
    public long Id { get; set; }
    public string? SerialNumberCode { get; set; }
    public string IssueNumberBefore { get; set; } = string.Empty;
    public string IssueNumberAfter { get; set; } = string.Empty;
    public int Qty { get; set; }
    public string? Note { get; set; }

    public bool Status { get; set; }
    public string Disposition { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
public class CreateStockInReworkDto
{
    public string SerialNumberCode { get; set; } = string.Empty;
    public List<IssueNumberRequestDto> IssueNumbers { get; set; } = new();
}

public class IssueNumberRequestDto
{
    public string IssueNumber { get; set; } = string.Empty;
    public string? Note { get; set; }
    public bool Status { get; set; }
}

public class UpdateStockInReworkDto
{
    public string? Disposition { get; set; }
}


public class FilterStockInReworkDto
{
    public long? SerialNumberId { get; set; }
    public DispositionType? Disposition { get; set; } 
}


public enum DispositionType
{
    PENDING,
    STOCK_IN,
    SCRAP
}