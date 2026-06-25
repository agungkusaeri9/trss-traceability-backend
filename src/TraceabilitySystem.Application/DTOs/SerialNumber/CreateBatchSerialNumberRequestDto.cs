namespace TraceabilitySystem.Application.DTOs.SerialNumber;

public class CreateBatchSerialNumberRequestDto
{
    public string Type { get; set; } = string.Empty;
    public List<string> SerialNumberCodes { get; set; } = new();
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Nomor-nomor issue yang akan dikurangi qty-nya (masing-masing 1 unit per SN).
    /// Opsional — jika tidak diisi, konsumsi issue tidak dilakukan.
    /// </summary>
    public List<string>? IssueNumbers { get; set; }
}
