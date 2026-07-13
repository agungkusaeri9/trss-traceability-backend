using System;
using TraceabilitySystem.Application.DTOs.StockIn;

namespace TraceabilitySystem.Application.DTOs.SerialNumber;

public class SerialNumberDto
{
    public int Id { get; set; }

    public string SerialNumberCode { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public List<SerialNumberIssueDto> Issues { get; set; } = [];

    public DateTime CreatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? UpdatedBy { get; set; }
}

public class SerialNumberIssueDto
{
    public int Id { get; set; }

    public string Number { get; set; } = string.Empty;

    public int StockInId { get; set; }

    public string PartNumber { get; set; } = string.Empty;

    public string PartName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}