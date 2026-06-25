using Mapster;
using TraceabilitySystem.Application.DTOs.SerialNumber;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Domain.Entities;
using TraceabilitySystem.Domain.Interfaces;
using TraceabilitySystem.Shared.Exceptions;

namespace TraceabilitySystem.Application.Services;

public class SerialNumberService : ISerialNumberService
{
    private readonly ISerialNumberRepository _serialNumberRepository;
    private readonly IIssueService _issueService;
    private readonly IIssueRepository _issueRepository;

    public SerialNumberService(
        ISerialNumberRepository serialNumberRepository,
        IIssueService issueService,
        IIssueRepository issueRepository)
    {
        _serialNumberRepository = serialNumberRepository;
        _issueService = issueService;
        _issueRepository = issueRepository;
    }

    public async Task<SerialNumberDto> CreateAsync(
        CreateSerialNumberRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var exists = await _serialNumberRepository.ExistsAsync(
            s => s.SerialNumberCode == request.SerialNumberCode, cancellationToken);

        if (exists)
            throw new AppException("Serial number sudah terdaftar.", 409);

        var serialNumber = request.Adapt<SerialNumber>();
        await _serialNumberRepository.AddAsync(serialNumber, cancellationToken);
        await _serialNumberRepository.SaveChangesAsync(cancellationToken);

        return serialNumber.Adapt<SerialNumberDto>();
    }

    public async Task<IEnumerable<SerialNumberDto>> CreateBatchAsync(
        CreateBatchSerialNumberRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.SerialNumberCodes == null || request.SerialNumberCodes.Count == 0)
            throw new AppException("Daftar serial number tidak boleh kosong.", 400);

        // Deduplicate dari request
        var uniqueCodes = request.SerialNumberCodes.Distinct().ToList();

        // Cek serial number yang sudah ada di DB
        var existingCodes = (await _serialNumberRepository.FindAsync(
            s => uniqueCodes.Contains(s.SerialNumberCode), cancellationToken))
            .Select(s => s.SerialNumberCode)
            .ToHashSet();

        var newEntities = uniqueCodes
            .Where(code => !existingCodes.Contains(code))
            .Select(code => new SerialNumber
            {
                SerialNumberCode = code,
                Type = request.Type,
                CreatedBy = request.CreatedBy
            })
            .ToList();

        if (newEntities.Count == 0)
            throw new AppException("Semua serial number sudah terdaftar.", 409);

        await _serialNumberRepository.AddRangeAsync(newEntities, cancellationToken);
        await _serialNumberRepository.SaveChangesAsync(cancellationToken);

        // Kurangi qty & catat issue transaction untuk setiap issue number yang disertakan
        if (request.IssueNumbers != null && request.IssueNumbers.Count > 0)
        {
            var issuesInDb = await _issueRepository.FindAsync(
                i => request.IssueNumbers.Contains(i.Number), cancellationToken);

            foreach (var serialNumber in newEntities)
            {
                foreach (var issue in issuesInDb)
                {
                    serialNumber.Issues.Add(new SerialNumberIssue
                    {
                        IssueId = issue.Id,
                        CreatedBy = request.CreatedBy,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            // Simpan relasi SerialNumberIssue ke database
            await _serialNumberRepository.SaveChangesAsync(cancellationToken);

            await _issueService.ConsumeBatchIssueAsync(
                request.IssueNumbers,
                qty: newEntities.Count,
                remark: $"Serial number batch create ({newEntities.Count} pcs)",
                cancellationToken: cancellationToken);
        }

        return newEntities.Adapt<IEnumerable<SerialNumberDto>>();
    }

    public async Task<IEnumerable<SerialNumberDto>> CreateFromIssuesAsync(
        CreateSerialNumbersFromIssuesRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.IssueNumbers == null || request.IssueNumbers.Count == 0)
            throw new AppException("Daftar issue number tidak boleh kosong.", 400);

        if (request.Qty <= 0)
            throw new AppException("Jumlah quantity harus minimal 1.", 400);

        // Tentukan Type dan prefix
        string type = request.Type;
        if (string.IsNullOrWhiteSpace(type))
        {
            type = "CLINCHING";
        }

        string prefix = "CC";
        if (type.Equals("MFanAssy", StringComparison.OrdinalIgnoreCase) || type.Contains("MF"))
        {
            type = "MFanAssy";
            prefix = "MF";
        }
        else
        {
            type = "CLINCHING";
            prefix = "CC";
        }

        // Generate serial numbers menggunakan GenerateByPrefixAsync
        return await GenerateByPrefixAsync(
            prefix,
            type,
            new GenerateSerialNumberRequestDto
            {
                Type = type,
                Qty = request.Qty,
                CreatedBy = request.CreatedBy,
                IssueNumbers = request.IssueNumbers
            },
            cancellationToken);
    }

    /// <summary>
    /// Generate serial number Clinching otomatis dengan format CC{yyyyMMdd}{sequence:D3}.
    /// Sequence dimulai dari 001 per hari dan bertambah sesuai data yang sudah ada di DB.
    /// </summary>
    public Task<IEnumerable<SerialNumberDto>> CreateByClinchingAsync(
        GenerateSerialNumberRequestDto request,
        CancellationToken cancellationToken = default)
        => GenerateByPrefixAsync("CC", "CLINCHING", request, cancellationToken);

    /// <summary>
    /// Generate serial number MFan otomatis dengan format MF{yyyyMMdd}{sequence:D3}.
    /// Sequence dimulai dari 001 per hari dan bertambah sesuai data yang sudah ada di DB.
    /// </summary>
    public Task<IEnumerable<SerialNumberDto>> CreateByMFanAsync(
        GenerateSerialNumberRequestDto request,
        CancellationToken cancellationToken = default)
        => GenerateByPrefixAsync("MF", "MFanAssy", request, cancellationToken);

    public async Task<SerialNumberDto?> GetBySerialNumberAsync(
        string serialNumber,
        CancellationToken cancellationToken = default)
    {
        var entity = await _serialNumberRepository.FirstOrDefaultAsync(
            s => s.SerialNumberCode == serialNumber, cancellationToken);

        return entity?.Adapt<SerialNumberDto>();
    }

    // ─── Private Helpers ────────────────────────────────────────────────────────

    /// <summary>
    /// Core logic untuk generate serial number berdasarkan prefix mesin.
    /// Format: {prefix}{yyyyMMdd}{sequence:D3}  →  contoh: CC20260625001
    /// </summary>
    private async Task<IEnumerable<SerialNumberDto>> GenerateByPrefixAsync(
        string machinePrefix,
        string type,
        GenerateSerialNumberRequestDto request,
        CancellationToken cancellationToken)
    {
        if (request.Qty <= 0)
            throw new AppException("Jumlah serial number harus lebih dari 0.", 400);

        var today = DateTime.UtcNow.ToString("yyyyMMdd");
        var datePrefix = $"{machinePrefix}{today}";   // contoh: CC20260625

        // Ambil semua SN hari ini untuk prefix ini
        var existingToday = await _serialNumberRepository.FindAsync(
            s => s.SerialNumberCode.StartsWith(datePrefix), cancellationToken);

        // Cari sequence tertinggi yang sudah ada hari ini
        var maxSequence = existingToday
            .Select(s =>
            {
                var suffix = s.SerialNumberCode[datePrefix.Length..]; // ambil bagian setelah date prefix
                return int.TryParse(suffix, out var seq) ? seq : 0;
            })
            .DefaultIfEmpty(0)
            .Max();

        // Generate kode baru
        var newCodes = Enumerable.Range(maxSequence + 1, request.Qty)
            .Select(seq => $"{datePrefix}{seq:D3}")
            .ToList();

        // Delegasikan ke CreateBatchAsync (dedup + insert + consume issue jika ada)
        return await CreateBatchAsync(new CreateBatchSerialNumberRequestDto
        {
            Type = type,
            SerialNumberCodes = newCodes,
            CreatedBy = request.CreatedBy,
            IssueNumbers = request.IssueNumbers
        }, cancellationToken);
    }
}
