namespace TraceabilitySystem.Application.DTOs.Issue;

/// <summary>
/// Request untuk mengurangi qty issue (konsumsi material).
/// </summary>
public class ConsumeIssueRequestDto
{
    /// <summary>Nomor issue yang akan dikonsumsi, contoh: "20260625001".</summary>
    public string IssueNumber { get; set; } = string.Empty;

    /// <summary>Jumlah qty yang dikonsumsi (harus > 0).</summary>
    public decimal QtyConsumed { get; set; }

    /// <summary>Keterangan tambahan (opsional).</summary>
    public string? Remark { get; set; }
}
