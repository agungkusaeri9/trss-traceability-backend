using TraceabilitySystem.Application.DTOs.Issue;

namespace TraceabilitySystem.Application.Interfaces;

public interface IIssueService
{
    /// <summary>
    /// Mengurangi qty issue berdasarkan nomor issue, lalu mencatat IssueTransaction.
    /// Qty dihitung dari transaksi terakhir (QtyAfter terakhir) sebagai QtyBefore.
    /// Melempar AppException jika:
    ///   - issue tidak ditemukan
    ///   - stok tidak mencukupi
    /// </summary>
    Task<IssueTransactionDto> ConsumeIssueAsync(
        ConsumeIssueRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Mengurangi qty dari beberapa issue sekaligus, masing-masing dikurangi sejumlah qty.
    /// Digunakan oleh SerialNumberService saat batch create serial number.
    /// </summary>
    Task<IEnumerable<IssueTransactionDto>> ConsumeBatchIssueAsync(
        IEnumerable<string> issueNumbers,
        int qty = 1,
        string? remark = null,
        CancellationToken cancellationToken = default);
}
