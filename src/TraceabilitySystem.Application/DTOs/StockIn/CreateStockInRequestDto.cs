using System;

namespace TraceabilitySystem.Application.DTOs.StockIn;

public class CreateStockInRequestDto
{
    public int PartId { get; set; }
    public int SupplyQty { get; set; }
    public DateTime SupplyDate { get; set; }
    public int ReceiptQty { get; set; }
    public DateTime ReceiptDate { get; set; }
}
