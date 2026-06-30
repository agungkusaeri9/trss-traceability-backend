namespace TraceabilitySystem.Application.DTOs.SerialNumber;

public class GenerateSerialNumberRequestDto
{
    /// <summary>Tipe proses (CLINCHING atau MFanAssy).</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Jumlah serial number yang akan digenerate.</summary>
    public int Qty { get; set; } = 1;

    /// <summary>Username yang melakukan generate.</summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Nomor-nomor issue yang akan dikurangi qty-nya (masing-masing 1 unit per SN).
    /// Opsional — jika tidak diisi, konsumsi issue tidak dilakukan.
    /// </summary>
    public List<string>? IssueNumbers { get; set; }

    /// <summary>
    /// Jika true, issue tidak akan dikurangi qty-nya meskipun IssueNumbers diisi.
    /// Default false.
    /// </summary>
    public bool SkipConsume { get; set; } = false;
}
