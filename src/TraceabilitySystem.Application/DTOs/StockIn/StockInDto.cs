using System;
using System.Collections.Generic;
using TraceabilitySystem.Application.DTOs.Part;

namespace TraceabilitySystem.Application.DTOs.StockIn;

public class StockInDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public int PartId { get; set; }
    public PartDto? Part { get; set; }

    public int SupplyQty { get; set; }
    public DateTime SupplyDate { get; set; }
    public int ReceiptQty { get; set; }
    public DateTime ReceiptDate { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public List<IssueDto> Issues { get; set; } = new List<IssueDto>();
}
