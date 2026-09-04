namespace TraceabilitySystem.Application.DTOs.SystemLog;

public enum SystemLogLevel
{
    INF,
    WRN,
    ERR,
    DBG,
    FTL,
    VRB
}

public class SystemLogFilterDto
{
    /// <summary>
    /// Tanggal spesifik log (format yyyy-MM-dd), contoh: '2026-09-04'.
    /// Jika tidak diisi, default akan menggunakan tanggal hari ini atau rentang tanggal.
    /// </summary>
    public string? Date { get; set; }

    /// <summary>
    /// Rentang tanggal awal (format yyyy-MM-dd)
    /// </summary>
    public string? StartDate { get; set; }

    /// <summary>
    /// Rentang tanggal akhir (format yyyy-MM-dd)
    /// </summary>
    public string? EndDate { get; set; }

    /// <summary>
    /// Pilihan level log: INF (Information), WRN (Warning), ERR (Error), DBG (Debug), FTL (Fatal), VRB (Verbose)
    /// </summary>
    public SystemLogLevel? Level { get; set; }

    /// <summary>
    /// Filter service: 'TraceabilitySystem.API', 'TraceabilitySystem.Worker', 'TraceabilitySystem.Backup', atau partial seperti 'API', 'worker'
    /// </summary>
    public string? Service { get; set; }

    /// <summary>
    /// Filter category: contoh 'Authentication', 'API', 'BackgroundJob'
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Pencarian keyword pada pesan log, category, correlation id, atau request id
    /// </summary>
    public string? Search { get; set; }

    public int Page { get; set; } = 1;
    public int Limit { get; set; } = 50;

    /// <summary>
    /// Urutan timestamp: 'desc' (terbaru dulu, default) atau 'asc' (terlama dulu)
    /// </summary>
    public string SortOrder { get; set; } = "desc";
}
