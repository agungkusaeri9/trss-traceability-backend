using System;

namespace TraceabilitySystem.Domain.Entities;

public class ProcessHistory
{
    public long Id { get; set; }

    /// <summary>
    /// Jenis referensi: "ProcessLog" atau "TraceabilityLog"
    /// </summary>
    public string ReferenceType { get; set; } = ProcessHistoryReferenceTypes.ProcessLog;

    /// <summary>
    /// ID dari referensi (ProcessLogId atau TraceabilityLogId)
    /// </summary>
    public long? ReferenceId { get; set; }

    /// <summary>
    /// Kode referensi (misalnya Serial Number Code atau Traceability Log Code)
    /// </summary>
    public string? ReferenceCode { get; set; }

    /// <summary>
    /// Nullable: FK ke ProcessLog (jika sumbernya dari ProcessLog)
    /// </summary>
    public long? ProcessLogId { get; set; }
    public virtual ProcessLog? ProcessLog { get; set; }

    /// <summary>
    /// Nullable: FK ke TraceabilityLog (jika sumbernya dari TraceabilityLog / ECM / Final)
    /// </summary>
    public long? TraceabilityLogId { get; set; }
    public virtual TraceabilityLog? TraceabilityLog { get; set; }

    /// <summary>
    /// Nullable: FK ke SerialNumber
    /// </summary>
    public int? SerialNumberId { get; set; }
    public virtual SerialNumber? SerialNumber { get; set; }

    /// <summary>
    /// Master Process (CLINCHING_SHORT_SIDE, ECM_ASSY, FINAL_INSPECTION, dll.)
    /// </summary>
    public int ProcessId { get; set; }
    public virtual Process Process { get; set; } = null!;

    /// <summary>
    /// Master Parameter (CORE_ASM_VALUE, ECM_STATUS, FINAL_STATUS, dll.)
    /// </summary>
    public int ParameterId { get; set; }
    public virtual Parameter Parameter { get; set; } = null!;

    public string? Value { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public static class ProcessHistoryReferenceTypes
{
    public const string ProcessLog = "ProcessLog";
    public const string TraceabilityLog = "TraceabilityLog";
}
