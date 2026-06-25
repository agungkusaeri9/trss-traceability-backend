namespace TraceabilitySystem.Application.DTOs.SerialNumber;

/// <summary>
/// Request untuk membuat serial number dari daftar issue number.
/// Qty per issue default = 1.
/// </summary>
public class CreateSerialNumbersFromIssuesRequestDto
{
    public List<string> IssueNumbers { get; set; } = new();
    public int Qty { get; set; } = 1;
    public string Type { get; set; } = string.Empty;
    public string? CreatedBy { get; set; }
}
