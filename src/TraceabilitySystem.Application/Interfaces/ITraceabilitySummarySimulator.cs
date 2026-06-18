using System.Collections.Generic;
using TraceabilitySystem.Application.DTOs.Dashboard;

namespace TraceabilitySystem.Application.Interfaces;

public interface ITraceabilitySummarySimulator
{
    int RevealedCount { get; }
    int TotalFields { get; }
    bool IsComplete { get; }

    /// <summary>Reveal next field and freeze its value. Returns true if a field was revealed.</summary>
    bool AdvanceReveal();

    /// <summary>Count down after all fields are revealed. Returns true when cycle resets.</summary>
    bool TickCooldown();

    List<DashboardSummaryFieldDto> GetSnapshot();
    void Reset();
}
