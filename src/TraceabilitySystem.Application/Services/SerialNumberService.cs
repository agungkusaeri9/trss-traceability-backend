using Mapster;
using TraceabilitySystem.Application.DTOs.Issue;
using TraceabilitySystem.Application.DTOs.Parameter;
using TraceabilitySystem.Application.DTOs.SerialNumber;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Domain.Entities;
using TraceabilitySystem.Domain.Interfaces;
using TraceabilitySystem.Shared.Exceptions;
using TraceabilitySystem.Shared.Models;

namespace TraceabilitySystem.Application.Services;

public class SerialNumberService : ISerialNumberService
{
    private readonly ISerialNumberRepository _serialNumberRepository;
    private readonly IIssueService _issueService;
    private readonly IIssueRepository _issueRepository;
    private readonly IMqttPublisher _mqttPublisher;

    public SerialNumberService(
        ISerialNumberRepository serialNumberRepository,
        IIssueService issueService,
        IIssueRepository issueRepository,
        IMqttPublisher mqttPublisher)
    {
        _serialNumberRepository = serialNumberRepository;
        _issueService = issueService;
        _issueRepository = issueRepository;
        _mqttPublisher = mqttPublisher;
    }


    public async Task<PagedResult<SerialNumberDto>> GetSerialNumbersAsync(
        int page, int pageSize, string? searchTerm = null, CancellationToken cancellationToken = default)
    {       

        var (serialNumbers, totalCount) = await _serialNumberRepository.GetPagedAsync(
            page,
            pageSize,
          predicate: p => (string.IsNullOrEmpty(searchTerm)
                || p.SerialNumberCode.Contains(searchTerm))
                && (p.SerialNumberCode.StartsWith("CC")),
            orderBy: q => q.OrderByDescending(u => u.CreatedAt),
            cancellationToken: cancellationToken);

        return new PagedResult<SerialNumberDto>
        {
            Items = serialNumbers.Adapt<IEnumerable<SerialNumberDto>>(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
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
            var issuesInDb = (await _issueRepository.FindAsync(
                i => request.IssueNumbers.Contains(i.Number), cancellationToken)).ToList();

            var isClinching = request.Type.Equals("CLINCHING", StringComparison.OrdinalIgnoreCase) || request.Type.StartsWith("CC", StringComparison.OrdinalIgnoreCase);

            if (isClinching)
            {
                // Untuk Clinching: setiap serial number mendapatkan SEMUA issue
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
            }
            else
            {
                // Untuk MFanAssy: setiap serial number mendapatkan SATU issue secara bergiliran
                for (int i = 0; i < newEntities.Count; i++)
                {
                    var issue = issuesInDb[i % issuesInDb.Count];
                    newEntities[i].Issues.Add(new SerialNumberIssue
                    {
                        IssueId = issue.Id,
                        CreatedBy = request.CreatedBy,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            // Simpan relasi SerialNumberIssue ke database
            await _serialNumberRepository.SaveChangesAsync(cancellationToken);

            if (isClinching)
            {
                // Untuk Clinching: setiap issue digunakan di semua serial number
                await _issueService.ConsumeBatchIssueAsync(
                    request.IssueNumbers,
                    qty: newEntities.Count,
                    remark: $"Serial number batch create ({newEntities.Count} pcs)",
                    cancellationToken: cancellationToken);
            }
            else
            {
                // Untuk MFanAssy: hitung berapa kali setiap issue digunakan
                var issueUsageCount = new Dictionary<string, int>();
                for (int i = 0; i < newEntities.Count; i++)
                {
                    var issue = issuesInDb[i % issuesInDb.Count];
                    if (!issueUsageCount.ContainsKey(issue.Number))
                    {
                        issueUsageCount[issue.Number] = 0;
                    }
                    issueUsageCount[issue.Number]++;
                }

                // Consume setiap issue sesuai dengan qty yang sesuai
                foreach (var kvp in issueUsageCount)
                {
                    await _issueService.ConsumeIssueAsync(new ConsumeIssueRequestDto
                    {
                        IssueNumber = kvp.Key,
                        QtyConsumed = kvp.Value,
                        Remark = $"Serial number batch create ({newEntities.Count} pcs)"
                    }, cancellationToken);
                }
            }
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
    /// Setiap serial number akan mendapatkan SEMUA issue yang diberikan.
    /// </summary>
    public async Task<IEnumerable<SerialNumberDto>> CreateByClinchingAsync(
        GenerateSerialNumberRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.Qty <= 0)
            throw new AppException("Jumlah serial number harus lebih dari 0.", 400);

        var today = DateTime.UtcNow.ToString("yyyyMMdd");
        var datePrefix = $"CC{today}";

        // Ambil semua SN hari ini untuk prefix ini
        var existingToday = await _serialNumberRepository.FindAsync(
            s => s.SerialNumberCode.StartsWith(datePrefix), cancellationToken);

        // Cari sequence tertinggi yang sudah ada hari ini
        var maxSequence = existingToday
            .Select(s =>
            {
                var suffix = s.SerialNumberCode[datePrefix.Length..]; 
                return int.TryParse(suffix, out var seq) ? seq : 0;
            })
            .DefaultIfEmpty(0)
            .Max();

        // Generate kode baru
        var newCodes = Enumerable.Range(maxSequence + 1, request.Qty)
            .Select(seq => $"{datePrefix}{seq:D3}")
            .ToList();

        // Deduplicate dari request dan cek existing di DB
        var uniqueCodes = newCodes.Distinct().ToList();
        var existingCodes = (await _serialNumberRepository.FindAsync(
            s => uniqueCodes.Contains(s.SerialNumberCode), cancellationToken))
            .Select(s => s.SerialNumberCode)
            .ToHashSet();

        var newEntities = uniqueCodes
            .Where(code => !existingCodes.Contains(code))
            .Select(code => new SerialNumber
            {
                SerialNumberCode = code,
                Type = "CLINCHING",
                CreatedBy = request.CreatedBy
            })
            .ToList();

        if (newEntities.Count == 0)
            throw new AppException("Semua serial number sudah terdaftar.", 409);

        // Create serial numbers with issue relations in repository using raw SQL query
        await _serialNumberRepository.CreateWithIssuesAsync(newEntities, request.IssueNumbers ?? new List<string>(), cancellationToken);

        //create process log
        // await _processLogService.CreateProcessLogByProcessAsync(
        //     "CLINCHING",
        //     "CC",
        //     newEntities.First().SerialNumberCode,
        //     request.Qty,
        //     request.CreatedBy,
        //     request.IssueNumbers
        // );

        // Consume issues (setiap issue dikonsumsi sebanyak jumlah serial number)
        IList<IssueTransactionDto> consumedIssues = new List<IssueTransactionDto>();
        if (request.IssueNumbers != null && request.IssueNumbers.Count > 0)
        {
            var batchResult = await _issueService.ConsumeBatchIssueAsync(
                request.IssueNumbers,
                qty: newEntities.Count,
                remark: $"Serial number batch create (Clinching, {newEntities.Count} pcs)",
                cancellationToken: cancellationToken);
            consumedIssues = batchResult.ToList();
        }

        // Publish MQTT notification setelah berhasil
        var mqttPayload = BuildClinchingMqttPayload(
            serialNumber: newEntities.First().SerialNumberCode,
            operatorUsername: request.CreatedBy ?? string.Empty,
            consumedIssues: consumedIssues);


        Console.WriteLine("MQTT Payload: " + mqttPayload);

        await _mqttPublisher.PublishAsync(
            "data/process/clinching-short-side/process",
            mqttPayload,
            cancellationToken);

        return newEntities.Adapt<IEnumerable<SerialNumberDto>>();
    }

    /// <summary>
    /// Membangun payload MQTT untuk notifikasi hasil proses clinching.
    /// Maksimal 3 issue dipetakan ke slot core_asm, upper_tank_asm, lower_tank_asm secara berurutan.
    /// </summary>
    private static object BuildClinchingMqttPayload(
        string serialNumber,
        string operatorUsername,
        IList<IssueTransactionDto> consumedIssues)
    {
        string? coreAsmIssueNo         = consumedIssues.Count > 0 ? consumedIssues[0].IssueNumber : null;
        decimal? coreAsmQtyRemaining   = consumedIssues.Count > 0 ? consumedIssues[0].QtyAfter    : null;
        string? upperTankIssueNo       = consumedIssues.Count > 1 ? consumedIssues[1].IssueNumber : null;
        decimal? upperTankQtyRemaining = consumedIssues.Count > 1 ? consumedIssues[1].QtyAfter    : null;
        string? lowerTankIssueNo       = consumedIssues.Count > 2 ? consumedIssues[2].IssueNumber : null;
        decimal? lowerTankQtyRemaining = consumedIssues.Count > 2 ? consumedIssues[2].QtyAfter    : null;

        return new
        {
            serial_number = serialNumber,
            operator_username = operatorUsername,
            data = new
            {
                core_asm_issue_no         = coreAsmIssueNo,
                core_asm_qty_remaining    = coreAsmQtyRemaining,
                upper_tank_asm_issue_no   = upperTankIssueNo,
                upper_tank_asm_qty_remaining = upperTankQtyRemaining,
                lower_tank_asm_issue_no   = lowerTankIssueNo,
                lower_tank_asm_qty_remaining = lowerTankQtyRemaining
            }
        };
    }

    /// <summary>
    /// Generate serial number MFan otomatis dengan format MF{yyyyMMdd}{sequence:D3}.
    /// Sequence dimulai dari 001 per hari dan bertambah sesuai data yang sudah ada di DB.
    /// Setiap serial number mendapatkan SATU issue secara bergiliran.
    /// </summary>
    public async Task<IEnumerable<SerialNumberDto>> CreateByMFanAsync(
        GenerateSerialNumberRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.Qty <= 0)
            throw new AppException("Jumlah serial number harus lebih dari 0.", 400);

        var today = DateTime.UtcNow.ToString("yyyyMMdd");
        var datePrefix = $"MF{today}";

        // Ambil semua SN hari ini untuk prefix ini
        var existingToday = await _serialNumberRepository.FindAsync(
            s => s.SerialNumberCode.StartsWith(datePrefix), cancellationToken);

        // Cari sequence tertinggi yang sudah ada hari ini
        var maxSequence = existingToday
            .Select(s =>
            {
                var suffix = s.SerialNumberCode[datePrefix.Length..]; 
                return int.TryParse(suffix, out var seq) ? seq : 0;
            })
            .DefaultIfEmpty(0)
            .Max();

        // Generate kode baru
        var newCodes = Enumerable.Range(maxSequence + 1, request.Qty)
            .Select(seq => $"{datePrefix}{seq:D3}")
            .ToList();

        // Deduplicate dan cek existing di DB
        var uniqueCodes = newCodes.Distinct().ToList();
        var existingCodes = (await _serialNumberRepository.FindAsync(
            s => uniqueCodes.Contains(s.SerialNumberCode), cancellationToken))
            .Select(s => s.SerialNumberCode)
            .ToHashSet();

        var newEntities = uniqueCodes
            .Where(code => !existingCodes.Contains(code))
            .Select(code => new SerialNumber
            {
                SerialNumberCode = code,
                Type = "MFanAssy",
                CreatedBy = request.CreatedBy
            })
            .ToList();

        if (newEntities.Count == 0)
            throw new AppException("Semua serial number sudah terdaftar.", 409);

        await _serialNumberRepository.AddRangeAsync(newEntities, cancellationToken);
        await _serialNumberRepository.SaveChangesAsync(cancellationToken);

        // Jika ada issue numbers, buat relasi SerialNumberIssue
        if (request.IssueNumbers != null && request.IssueNumbers.Count > 0)
        {
            var issuesInDb = (await _issueRepository.FindAsync(
                i => request.IssueNumbers.Contains(i.Number), cancellationToken)).ToList();

            var issueUsageCount = new Dictionary<string, int>();

            if (newEntities.Count == 1)
            {
                // Single serial number → semua issue dikaitkan ke SN tersebut
                foreach (var issue in issuesInDb)
                {
                    newEntities[0].Issues.Add(new SerialNumberIssue
                    {
                        IssueId = issue.Id,
                        CreatedBy = request.CreatedBy,
                        CreatedAt = DateTime.UtcNow
                    });

                    if (!issueUsageCount.ContainsKey(issue.Number))
                        issueUsageCount[issue.Number] = 0;
                    issueUsageCount[issue.Number]++;
                }
            }
            else
            {
                // Batch → setiap serial number mendapatkan SATU issue secara bergiliran
                for (int i = 0; i < newEntities.Count; i++)
                {
                    var issue = issuesInDb[i % issuesInDb.Count];
                    newEntities[i].Issues.Add(new SerialNumberIssue
                    {
                        IssueId = issue.Id,
                        CreatedBy = request.CreatedBy,
                        CreatedAt = DateTime.UtcNow
                    });

                    if (!issueUsageCount.ContainsKey(issue.Number))
                        issueUsageCount[issue.Number] = 0;
                    issueUsageCount[issue.Number]++;
                }
            }

            await _serialNumberRepository.SaveChangesAsync(cancellationToken);

            // Consume issues sesuai usage count (skip jika SkipConsume = true)
            if (!request.SkipConsume)
            {
                foreach (var kvp in issueUsageCount)
                {
                    await _issueService.ConsumeIssueAsync(new ConsumeIssueRequestDto
                    {
                        IssueNumber = kvp.Key,
                        QtyConsumed = kvp.Value,
                        Remark = $"Serial number batch create (MFan, {newEntities.Count} pcs)"
                    }, cancellationToken);
                }
            }
        }

        return newEntities.Adapt<IEnumerable<SerialNumberDto>>();
    }

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
