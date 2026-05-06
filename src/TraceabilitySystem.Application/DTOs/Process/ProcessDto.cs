using System;
using System.Collections.Generic;
using TraceabilitySystem.Application.DTOs.Parameter;

namespace TraceabilitySystem.Application.DTOs.Process;

public class ProcessDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<ParameterDto> Parameters { get; set; } = new List<ParameterDto>();
}
