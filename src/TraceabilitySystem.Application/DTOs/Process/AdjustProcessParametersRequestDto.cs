using System.Collections.Generic;

namespace TraceabilitySystem.Application.DTOs.Process;

public class AdjustProcessParametersRequestDto
{
    public List<int> ParameterIds { get; set; } = new List<int>();
}
