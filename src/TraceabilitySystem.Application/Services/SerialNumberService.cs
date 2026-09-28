using Mapster;
using TraceabilitySystem.Application.DTOs.Issue;
using TraceabilitySystem.Application.DTOs.Parameter;
using TraceabilitySystem.Application.DTOs.SerialNumber;
using TraceabilitySystem.Application.DTOs.StockIn;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Application.Mappers;
using TraceabilitySystem.Domain.Entities;
using TraceabilitySystem.Domain.Interfaces;
using TraceabilitySystem.Shared.Exceptions;
using TraceabilitySystem.Shared.Helpers;
using TraceabilitySystem.Shared.Models;

namespace TraceabilitySystem.Application.Services;

public class SerialNumberService : ISerialNumberService
{
    private readonly ISerialNumberRepository _serialNumberRepository;
    private readonly IIssueService _issueService;
    private readonly IIssueRepository _issueRepository;
    private readonly IMqttPublisher _mqttPublisher;
    private readonly IAppConfigRepository _appConfigRepository;

    public SerialNumberService(
        ISerialNumberRepository serialNumberRepository,
        IIssueService issueService,
        IIssueRepository issueRepository,
        IMqttPublisher mqttPublisher,
        IAppConfigRepository appConfigRepository)
    {
        _serialNumberRepository = serialNumberRepository;
        _issueService = issueService;
        _issueRepository = issueRepository;
        _mqttPublisher = mqttPublisher;
        _appConfigRepository = appConfigRepository;
    }


    public async Task<PagedResult<SerialNumberDto>> GetSerialNumbersAsync(
        int page, int pageSize, string? searchTerm = null, bool? status = null, bool isFinished = true, CancellationToken cancellationToken = default)
    {       

        var (serialNumbers, totalCount) = await _serialNumberRepository.GetPagedWithRelatedAsync(
            page,
            pageSize,
          predicate: p => (string.IsNullOrEmpty(searchTerm)
                || p.SerialNumberCode.Contains(searchTerm))
                && (p.SerialNumberCode.StartsWith("PVRA") || p.SerialNumberCode.StartsWith("CC"))
                && p.ProcessLogs.Any(pl => pl.IsFinished == isFinished && (!status.HasValue || pl.Status == status.Value)),
            orderBy: q => q.OrderByDescending(u => u.CreatedAt),
            cancellationToken: cancellationToken);

        return new PagedResult<SerialNumberDto>
        {
            Items = serialNumbers.Select(x => x.ToDto()),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<SerialNumberDto> CreateAsync(
        CreateSerialNumberRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var today = DateTimeHelper.GetJakartaToday();
        var startOfYear = new DateOnly(today.Year, 1, 1);
        var endOfYear = new DateOnly(today.Year, 12, 31);

        var exists = await _serialNumberRepository.ExistsAsync(
            s => s.SerialNumberCode == request.SerialNumberCode && s.Date >= startOfYear && s.Date <= endOfYear, cancellationToken);

        if (exists)
            throw new AppException("Serial number sudah terdaftar untuk tahun ini.", 409);

        var serialNumber = request.Adapt<SerialNumber>();
        serialNumber.Date = today;
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

        var today = DateTimeHelper.GetJakartaToday();
        var startOfYear = new DateOnly(today.Year, 1, 1);
        var endOfYear = new DateOnly(today.Year, 12, 31);

        // Deduplicate dari request
        var uniqueCodes = request.SerialNumberCodes.Distinct().ToList();

        // Cek serial number yang sudah ada di DB pada tahun ini
        var existingCodes = (await _serialNumberRepository.FindAsync(
            s => uniqueCodes.Contains(s.SerialNumberCode) && s.Date >= startOfYear && s.Date <= endOfYear, cancellationToken))
            .Select(s => s.SerialNumberCode)
            .ToHashSet();

        var newEntities = uniqueCodes
            .Where(code => !existingCodes.Contains(code))
            .Select(code => new SerialNumber
            {
                SerialNumberCode = code,
                Type = request.Type,
                Date = today,
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

            var isClinching = request.Type.Equals("CLINCHING", StringComparison.OrdinalIgnoreCase) || 
                              request.Type.StartsWith("PVRA", StringComparison.OrdinalIgnoreCase) || 
                              request.Type.StartsWith("CC", StringComparison.OrdinalIgnoreCase);

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

        string prefix = "PVRA";
        if (type.Equals("MFanAssy", StringComparison.OrdinalIgnoreCase) || type.Contains("MF"))
        {
            type = "MFanAssy";
            prefix = "MF";
        }
        else
        {
            type = "CLINCHING";
            prefix = "PVRA";
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
    /// Generate serial number Clinching otomatis dengan format PVRA{sequence:D6} (contoh: PVRA000001).
    /// Sequence dimulai dari 000001 dan bertambah sesuai data PVRA yang sudah ada di DB.
    /// Setiap serial number akan mendapatkan SEMUA issue yang diberikan.
    /// </summary>
    public async Task<IEnumerable<SerialNumberDto>> CreateByClinchingAsync(
        GenerateSerialNumberRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.Qty <= 0)
            throw new AppException("Jumlah serial number harus lebih dari 0.", 400);

        var prefix = "PVRA";
        var today = DateTimeHelper.GetJakartaToday();
        var startOfYear = new DateOnly(today.Year, 1, 1);
        var endOfYear = new DateOnly(today.Year, 12, 31);

        var countSerialNumber = await _serialNumberRepository.CountAsync();

        // Ambil semua SN dengan prefix PVRA pada tahun berjalan
        var existingCodes = (await _serialNumberRepository.FindAsync(
            s => s.SerialNumberCode.StartsWith(prefix) && s.Date >= startOfYear && s.Date <= endOfYear, cancellationToken)).ToList();

        int startSequence;

        if(countSerialNumber < 1)
        {
            // Jika belum ada data sebelumnya di DB, ambil initial sequence dari AppConfig
                startSequence = await GetStartClinchingSequenceAsync(cancellationToken);
        }
        else
        {
           if (existingCodes.Count > 0)
            {
                // Cari sequence tertinggi yang sudah ada pada tahun berjalan (restart ke 0 jika tahun baru)
                var maxSequence = existingCodes
                    .Select(s =>
                    {
                        var suffix = s.SerialNumberCode[prefix.Length..]; 
                        return int.TryParse(suffix, out var seq) ? seq : 0;
                    })
                    .DefaultIfEmpty(0)
                    .Max();

                startSequence = maxSequence + 1;
            }
            else
            {
                startSequence = 1;
            } 
        }
        

        // Generate kode baru: PVRA000001, PVRA000002, dst. (atau sesuai startSequence)
        var newCodes = Enumerable.Range(startSequence, request.Qty)
            .Select(seq => $"{prefix}{seq:D6}")
            .ToList();

        // Deduplicate dari request dan cek existing di DB pada tahun berjalan
        var uniqueCodes = newCodes.Distinct().ToList();
        var existingCodesInDb = (await _serialNumberRepository.FindAsync(
            s => uniqueCodes.Contains(s.SerialNumberCode) && s.Date >= startOfYear && s.Date <= endOfYear, cancellationToken))
            .Select(s => s.SerialNumberCode)
            .ToHashSet();

        var newEntities = uniqueCodes
            .Where(code => !existingCodesInDb.Contains(code))
            .Select(code => new SerialNumber
            {
                SerialNumberCode = code,
                Type = "CLINCHING",
                Date = today,
                CreatedBy = request.CreatedBy
            })
            .ToList();

        if (newEntities.Count == 0)
            throw new AppException("Semua serial number sudah terdaftar pada tahun ini.", 409);

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

        return newEntities.Adapt<IEnumerable<SerialNumberDto>>();
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

        var today = DateTimeHelper.GetJakartaToday();
        var datePrefix = $"MF{today:yyyyMMdd}";

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

        // Deduplicate dan cek existing di DB untuk hari ini
        var uniqueCodes = newCodes.Distinct().ToList();
        var existingCodes = (await _serialNumberRepository.FindAsync(
            s => uniqueCodes.Contains(s.SerialNumberCode) && s.Date == today, cancellationToken))
            .Select(s => s.SerialNumberCode)
            .ToHashSet();

        var newEntities = uniqueCodes
            .Where(code => !existingCodes.Contains(code))
            .Select(code => new SerialNumber
            {
                SerialNumberCode = code,
                Type = "MFanAssy",
                Date = today,
                CreatedBy = request.CreatedBy
            })
            .ToList();

        if (newEntities.Count == 0)
            throw new AppException("Semua serial number sudah terdaftar untuk hari ini.", 409);

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
        var entity = await _serialNumberRepository
            .GetWithRelatedBySerialNumberAsync(serialNumber, cancellationToken);

        return entity?.ToDto();
    }

    // ─── Private Helpers ────────────────────────────────────────────────────────

    /// <summary>
    /// Core logic untuk generate serial number berdasarkan prefix mesin.
    /// Format:
    /// - PVRA / CLINCHING: PVRA{sequence:D6}  →  contoh: PVRA000001
    /// - MF / Lainnya: {prefix}{yyyyMMdd}{sequence:D3}  →  contoh: MF20260625001
    /// </summary>
    private async Task<IEnumerable<SerialNumberDto>> GenerateByPrefixAsync(
        string machinePrefix,
        string type,
        GenerateSerialNumberRequestDto request,
        CancellationToken cancellationToken)
    {
        if (request.Qty <= 0)
            throw new AppException("Jumlah serial number harus lebih dari 0.", 400);

        List<string> newCodes;
        var today = DateTimeHelper.GetJakartaToday();
        var startOfYear = new DateOnly(today.Year, 1, 1);
        var endOfYear = new DateOnly(today.Year, 12, 31);

        if (machinePrefix.Equals("PVRA", StringComparison.OrdinalIgnoreCase) || machinePrefix.Equals("CC", StringComparison.OrdinalIgnoreCase))
        {
            var prefix = "PVRA";
            var existing = (await _serialNumberRepository.FindAsync(
                s => s.SerialNumberCode.StartsWith(prefix) && s.Date >= startOfYear && s.Date <= endOfYear, cancellationToken)).ToList();

            int startSequence;
            if (existing.Count > 0)
            {
                var maxSequence = existing
                    .Select(s =>
                    {
                        var suffix = s.SerialNumberCode[prefix.Length..];
                        return int.TryParse(suffix, out var seq) ? seq : 0;
                    })
                    .DefaultIfEmpty(0)
                    .Max();

                startSequence = maxSequence + 1;
            }
            else
            {
                startSequence = await GetStartClinchingSequenceAsync(cancellationToken);
            }

            newCodes = Enumerable.Range(startSequence, request.Qty)
                .Select(seq => $"{prefix}{seq:D6}")
                .ToList();
        }
        else
        {
            var datePrefix = $"{machinePrefix}{today:yyyyMMdd}";

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
            newCodes = Enumerable.Range(maxSequence + 1, request.Qty)
                .Select(seq => $"{datePrefix}{seq:D3}")
                .ToList();
        }

        // Delegasikan ke CreateBatchAsync (dedup + insert + consume issue jika ada)
        return await CreateBatchAsync(new CreateBatchSerialNumberRequestDto
        {
            Type = type,
            SerialNumberCodes = newCodes,
            CreatedBy = request.CreatedBy,
            IssueNumbers = request.IssueNumbers
        }, cancellationToken);
    }

    public async Task<string> GenerateSerialNumberAsync(CancellationToken cancellationToken = default)
    {
        var prefix = "PVRA";
        var today = DateTimeHelper.GetJakartaToday();
        var startOfYear = new DateOnly(today.Year, 1, 1);
        var endOfYear = new DateOnly(today.Year, 12, 31);

        var existing = (await _serialNumberRepository.FindAsync(
            s => s.SerialNumberCode.StartsWith(prefix) && s.Date >= startOfYear && s.Date <= endOfYear, cancellationToken)).ToList();

        int nextSequence;
        if (existing.Count > 0)
        {
            var maxSequence = existing
                .Select(s =>
                {
                    var suffix = s.SerialNumberCode[prefix.Length..];
                    return int.TryParse(suffix, out var seq) ? seq : 0;
                })
                .DefaultIfEmpty(0)
                .Max();

            nextSequence = maxSequence + 1;
        }
        else
        {
            nextSequence = await GetStartClinchingSequenceAsync(cancellationToken);
        }

        var newCode = $"{prefix}{nextSequence:D6}";

        var entity = new SerialNumber
        {
            SerialNumberCode = newCode,
            Type = "CLINCHING",
            Date = today,
            CreatedBy = "SYSTEM"
        };

        await _serialNumberRepository.AddAsync(entity, cancellationToken);
        await _serialNumberRepository.SaveChangesAsync(cancellationToken);

        return newCode;
    }

    private async Task<int> GetStartClinchingSequenceAsync(CancellationToken cancellationToken = default)
    {
        var config = await _appConfigRepository.GetByKeyAsync("START_SERIAL_NUMBER_CLINCHING", cancellationToken);
        if (config == null || string.IsNullOrWhiteSpace(config.Value))
            return 1;

        var val = config.Value.Trim();
        if (val.StartsWith("PVRA", StringComparison.OrdinalIgnoreCase))
            val = val["PVRA".Length..];
        else if (val.StartsWith("CC", StringComparison.OrdinalIgnoreCase))
            val = val["CC".Length..];

        if (int.TryParse(val, out var startSeq) && startSeq > 0)
        {
            return startSeq;
        }

        return 1;
    }
}
