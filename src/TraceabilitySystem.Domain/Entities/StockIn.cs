using System;
using System.Collections.Generic;

namespace TraceabilitySystem.Domain.Entities;

public class StockIn
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public int PartId { get; set; }
    public Part? Part { get; set; }

    public int SupplyQty { get; set; }
    public DateTime SupplyDate { get; set; }
    public int ReceiptQty { get; set; }
    public DateTime ReceiptDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Issue> Issues { get; set; } = new List<Issue>();
}
