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

    /// <summary>
    /// Memetakan issue numbers ke dictionary parameter untuk proses Clinching
    /// berdasarkan nama Part atau kode Part dari database.
    /// Contoh parameter: CORE_ASM_VALUE, UPPER_TANK_ASM_VALUE, LOWER_TANK_ASM_VALUE.
    /// </summary>
    Task<Dictionary<string, object>> MapClinchingIssuesToParametersAsync(
        IEnumerable<string> issueNumbers,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Memetakan issue numbers ke dictionary parameter untuk proses M-Fan
    /// berdasarkan nama Part atau kode Part dari database.
    /// Contoh parameter: LOT_FAN_ASM_RESULT, LOT_MOTOR_ASM_RESULT, LOT_GUIDE_ASM_RESULT.
    /// </summary>
    Task<Dictionary<string, object>> MapMFanIssuesToParametersAsync(
        IEnumerable<string> issueNumbers,
        CancellationToken cancellationToken = default);
}
