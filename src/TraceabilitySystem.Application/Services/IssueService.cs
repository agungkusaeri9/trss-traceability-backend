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
        //    - Jika belum ada → pakai RemainingQty / ReceiptQty
        var lastTransaction = (await _transactionRepository.FindAsync(
                t => t.IssueId == issue.Id, cancellationToken))
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefault();

        decimal qtyBefore = lastTransaction?.QtyAfter ?? issue.RemainingQty ?? stockIn.RemainingQty ?? stockIn.ReceiptQty;

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

        // 5. Update RemainingQty pada Issue dan StockIn = QtyAfter
        issue.RemainingQty = qtyAfter;
        issue.UpdatedAt = DateTime.UtcNow;
        _issueRepository.Update(issue);
        await _issueRepository.SaveChangesAsync(cancellationToken);

        stockIn.RemainingQty = qtyAfter;
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
            return [];

        var results = new List<IssueTransactionDto>(numbers.Count);

        foreach (var number in numbers)
        {
            var dto = await ConsumeIssueAsync(new ConsumeIssueRequestDto
            {
                IssueNumber = number,
                QtyConsumed = qty,
                Remark = remark ?? "Serial number create"
            }, cancellationToken);

            results.Add(dto);
        }

        return results;
    }

    /// <inheritdoc/>
    public async Task<Dictionary<string, object>> MapClinchingIssuesToParametersAsync(
        IEnumerable<string> issueNumbers,
        CancellationToken cancellationToken = default)
    {
        var numbersList = issueNumbers.Distinct().ToList();
        if (numbersList.Count == 0)
            return [];

        var issues = await _issueRepository.GetByNumbersWithPartAsync(numbersList, cancellationToken);
        var result = new Dictionary<string, object>();

        foreach (var issue in issues)
        {
            var partName = issue.StockIn?.Part?.Name ?? string.Empty;
            var paramKey = ResolveClinchingParameterKey(partName);
            if (!string.IsNullOrEmpty(paramKey))
            {
                result[paramKey] = issue.Number;
            }
        }

        // Fallback jika parts belum lengkap terelasi di database
        if (!result.ContainsKey("CORE_ASM_VALUE") && numbersList.Count >= 3)
        {
            result["UPPER_TANK_ASM_VALUE"] = numbersList[0];
            result["LOWER_TANK_ASM_VALUE"] = numbersList[1];
            result["CORE_ASM_VALUE"]       = numbersList[2];
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<Dictionary<string, object>> MapMFanIssuesToParametersAsync(
        IEnumerable<string> issueNumbers,
        CancellationToken cancellationToken = default)
    {
        var numbersList = issueNumbers.Distinct().ToList();
        if (numbersList.Count == 0)
            return [];

        var issues = await _issueRepository.GetByNumbersWithPartAsync(numbersList, cancellationToken);
        var result = new Dictionary<string, object>();

        foreach (var issue in issues)
        {
            var partName = issue.StockIn?.Part?.Name ?? string.Empty;
            var paramKey = ResolveMFanParameterKey(partName);
            if (!string.IsNullOrEmpty(paramKey))
            {
                result[paramKey] = issue.Number;
            }
        }

        // Fallback jika parts belum lengkap terelasi di database
        if (!result.ContainsKey("LOT_FAN_ASM_RESULT") && numbersList.Count >= 3)
        {
            result["LOT_FAN_ASM_RESULT"]   = numbersList[0];
            result["LOT_MOTOR_ASM_RESULT"] = numbersList[1];
            result["LOT_GUIDE_ASM_RESULT"] = numbersList[2];
        }

        return result;
    }

    private static string? ResolveClinchingParameterKey(string partName)
    {
        if (partName.Contains("Upper", StringComparison.OrdinalIgnoreCase))
            return "UPPER_TANK_ASM_VALUE";
        if (partName.Contains("Lower", StringComparison.OrdinalIgnoreCase))
            return "LOWER_TANK_ASM_VALUE";
        if (partName.Contains("Core", StringComparison.OrdinalIgnoreCase))
            return "CORE_ASM_VALUE";

        return null;
    }

    private static string? ResolveMFanParameterKey(string partName)
    {
        if (partName.Contains("Motor", StringComparison.OrdinalIgnoreCase))
            return "LOT_MOTOR_ASM_RESULT";
        if (partName.Contains("Guide", StringComparison.OrdinalIgnoreCase))
            return "LOT_GUIDE_ASM_RESULT";
        if (partName.Contains("Fan", StringComparison.OrdinalIgnoreCase))
            return "LOT_FAN_ASM_RESULT";

        return null;
    }
}
