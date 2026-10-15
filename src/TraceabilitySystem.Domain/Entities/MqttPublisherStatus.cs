using System;

namespace TraceabilitySystem.Domain.Entities;

public class MqttPublisherStatus
{
    public long Id { get; set; }

    public string ProcessCode { get; set; } = string.Empty;

    public bool IsPublisher { get; set; } = true;

    /// <summary>
    /// Jenis referensi: "ProcessLog" atau "TraceabilityLog"
    /// </summary>
    public string ReferenceType { get; set; } = string.Empty;

    /// <summary>
    /// Kode data yang menjadi referensi (misalnya Serial Number Code atau Traceability Log Code)
    /// </summary>
    public string ReferenceCode { get; set; } = string.Empty;

    /// <summary>
    /// Status publikasi MQTT (misalnya: "Published", "Skipped", "Success", "Failed")
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Alasan / keterangan status publikasi MQTT
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Tanggal dan waktu status publikasi
    /// </summary>
    public DateTime Date { get; set; } = DateTime.UtcNow;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}

public static class MqttPublisherReferenceTypes
{
    public const string ProcessLog = "ProcessLog";
    public const string TraceabilityLog = "TraceabilityLog";
}
