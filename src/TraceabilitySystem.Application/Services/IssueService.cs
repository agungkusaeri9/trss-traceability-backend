using Mapster;
using TraceabilitySystem.Application.DTOs.Issue;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Domain.Entities;
using TraceabilitySystem.Domain.Interfaces;
using TraceabilitySystem.Shared.Exceptions;

namespace TraceabilitySystem.Application.Services;

public class IssueService : IIssueService
{
    private readonly IIssueRepository _issueRepository;
    private readonly IIssueTransactionRepository _transactionRepository;
    private readonly IStockInRepository _stockInRepository;

    public IssueService(
        IIssueRepository issueRepository,
        IIssueTransactionRepository transactionRepository,
        IStockInRepository stockInRepository)
    {
        _issueRepository = issueRepository;
        _transactionRepository = transactionRepository;
        _stockInRepository = stockInRepository;
    }

    /// <inheritdoc/>
    public async Task<IssueTransactionDto> ConsumeIssueAsync(
        ConsumeIssueRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.QtyConsumed <= 0)
            throw new AppException("QtyConsumed harus lebih dari 0.", 400);

        // 1. Cari issue (IssueRepository.FirstOrDefaultAsync sudah include StockIn)
        var issue = await _issueRepository.FirstOrDefaultAsync(
            i => i.Number == request.IssueNumber, cancellationToken)
            ?? throw new NotFoundException(nameof(Issue), request.IssueNumber);

        var stockIn = issue.StockIn
            ?? throw new AppException(
                $"StockIn tidak ditemukan untuk issue {request.IssueNumber}.", 404);

        // 2. Hitung QtyBefore:
        //    - Jika sudah ada transaksi sebelumnya → pakai QtyAfter transaksi terakhir
        //    - Jika belum ada → pakai ReceiptQty dari StockIn (stok awal)
        var lastTransaction = (await _transactionRepository.FindAsync(
                t => t.IssueId == issue.Id, cancellationToken))
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefault();

        decimal qtyBefore = lastTransaction?.QtyAfter ?? stockIn.ReceiptQty;

        // 3. Validasi stok cukup
        if (qtyBefore < request.QtyConsumed)
            throw new AppException(
                $"Stok tidak mencukupi untuk issue {request.IssueNumber}. " +
                $"Tersedia: {qtyBefore}, Diminta: {request.QtyConsumed}.", 422);

        var qtyAfter = qtyBefore - request.QtyConsumed;

        // 4. Catat IssueTransaction
        var transaction = new IssueTransaction
        {
            IssueId = issue.Id,
            QtyBefore = qtyBefore,
            QtyChange = -request.QtyConsumed,   // negatif = keluar
            QtyAfter = qtyAfter,
            Type = "ISSUE",
            Remark = request.Remark
        };

        await _transactionRepository.AddAsync(transaction, cancellationToken);
        await _transactionRepository.SaveChangesAsync(cancellationToken);

        // 5. Update ReceiptQty di StockIn = QtyAfter (qty tersisa)
        stockIn.ReceiptQty = (int)qtyAfter;
        stockIn.UpdatedAt = DateTime.UtcNow;
        _stockInRepository.Update(stockIn);
        await _stockInRepository.SaveChangesAsync(cancellationToken);

        var result = transaction.Adapt<IssueTransactionDto>();
        result.IssueNumber = request.IssueNumber;
        return result;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<IssueTransactionDto>> ConsumeBatchIssueAsync(
        IEnumerable<string> issueNumbers,
        int qty = 1,
        string? remark = null,
        CancellationToken cancellationToken = default)
    {
        var numbers = issueNumbers.Distinct().ToList();
        if (numbers.Count == 0)
            return Enumerable.Empty<IssueTransactionDto>();

        var results = new List<IssueTransactionDto>();

        // Proses satu per satu agar validasi stok & update StockIn per issue akurat
        foreach (var number in numbers)
        {
            var dto = await ConsumeIssueAsync(new ConsumeIssueRequestDto
            {
                IssueNumber = number,
                QtyConsumed = qty,        // Menggunakan qty yang diteruskan
                Remark = remark ?? "Serial number create"
            }, cancellationToken);

            results.Add(dto);
        }

        return results;
    }
}
