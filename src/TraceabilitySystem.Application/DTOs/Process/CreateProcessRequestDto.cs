using System.ComponentModel.DataAnnotations;

namespace TraceabilitySystem.Application.DTOs.Process;

public class CreateProcessRequestDto
{
    [Required(ErrorMessage = "Process Code is required.")]
    [StringLength(50, ErrorMessage = "Code cannot exceed 50 characters.")]
    public string Code { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
    public string? Name { get; set; }

    [StringLength(255, ErrorMessage = "Description cannot exceed 255 characters.")]
    public string? Description { get; set; }
}
