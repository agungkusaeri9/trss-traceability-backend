using TraceabilitySystem.Application.DTOs.Dashboard;
using TraceabilitySystem.Application.Interfaces;

namespace TraceabilitySystem.Application.Services;

public class TraceabilitySummarySimulator : ITraceabilitySummarySimulator
{
    private const int CooldownSeconds = 10;

    private static readonly FieldTemplate[] Fields =
    [
        new("nameLabelSerialNo", "Name Label Serial No", GenerateSerialNo),
        new("lotCoreAsm", "Lot Core Asm", seed => $"LOT-CORE-{GenerateLotSuffix(seed)}"),
        new("lotUpperTAsm", "Lot Upper T Asm", seed => $"LOT-UPPER-{GenerateLotSuffix(seed)}"),
        new("lotLowerTAsm", "Lot Lower T Asm", seed => $"LOT-LOWER-{GenerateLotSuffix(seed)}"),
        new("clinchingHeightBool", "Clinching Height", seed => seed % 5 != 0),
        new("clinchingHeight", "Clinching Height Values", GenerateClinchingHeights),
        new("endPlateWidthBool", "End Plate Width", seed => seed % 6 != 0),
        new("radCap", "Rad Cap", seed => $"RC-TRSS-{8000 + seed % 200}"),
        new("heLeakBool", "HE Leak", seed => seed % 7 != 0),
        new("heLeakLastLeakage", "HE Leak Last Leakage", seed => Math.Round(0.0015 + seed % 10 * 0.0001, 4)),
        new("mFanSebango", "M Fan Sebango", seed => $"SB-{GenerateLotSuffix(seed)}"),
        new("lotFanAsm", "Lot Fan Asm", seed => $"LOT-FAN-{GenerateLotSuffix(seed)}"),
        new("lotMotorAsm", "Lot Motor Asm", seed => $"LOT-MOTOR-{GenerateLotSuffix(seed)}"),
        new("lotGuideAsm", "Lot Guide Asm", seed => $"LOT-GUIDE-{GenerateLotSuffix(seed)}"),
        new("mFanTightenBolt", "M Fan Tighten Bolt", seed => Math.Round(8.0 + seed % 8 * 0.1, 1)),
        new("mFanTightenNut", "M Fan Tighten Nut", seed => Math.Round(6.0 + seed % 6 * 0.1, 1)),
        new("mFanRotationSpMax", "M Fan Rotation SP Max", seed => 2800 + seed % 20 * 5),
        new("mFanRotationSpMin", "M Fan Rotation SP Min", seed => 2700 + seed % 20 * 5),
        new("mFanAmpereMax", "M Fan Ampere Max", seed => Math.Round(2.2 + seed % 8 * 0.1, 1)),
        new("mFanAmpereMin", "M Fan Ampere Min", seed => Math.Round(2.0 + seed % 6 * 0.1, 1)),
        new("mFanWindDirection", "M Fan Wind Direction", seed => seed % 2 == 0 ? "CW" : "CCW"),
        new("mFanInsepctionBool", "M Fan Inspection", seed => seed % 4 != 0),
        new("ecmAssyBolthTightQty", "ECM Assy Bolt Tight Qty", seed => 6 + seed % 5),
        new("ecmAssyBolthTight", "ECM Assy Bolt Tight", _ => "OK"),
        new("finalInspection", "Final Inspection", _ => "OK"),
    ];

    private readonly object?[] _frozenValues = new object?[Fields.Length];

    private int _revealedCount;
    private int _cooldownSeconds;
    private int _cycle;

    public int RevealedCount => _revealedCount;
    public int TotalFields => Fields.Length;
    public bool IsComplete => _revealedCount >= Fields.Length;

    public bool AdvanceReveal()
    {
        if (IsComplete)
        {
            return false;
        }

        var seed = _cycle * 1000 + _revealedCount + Environment.TickCount;
        _frozenValues[_revealedCount] = Fields[_revealedCount].ValueGenerator(seed);
        _revealedCount++;
        return true;
    }

    public bool TickCooldown()
    {
        if (!IsComplete)
        {
            return false;
        }

        _cooldownSeconds++;

        if (_cooldownSeconds < CooldownSeconds)
        {
            return false;
        }

        Reset();
        return true;
    }

    public void Reset()
    {
        _revealedCount = 0;
        _cooldownSeconds = 0;
        _cycle++;
        Array.Clear(_frozenValues);
    }

    public List<DashboardSummaryFieldDto> GetSnapshot()
    {
        var summary = new List<DashboardSummaryFieldDto>(Fields.Length);

        for (var i = 0; i < Fields.Length; i++)
        {
            var field = Fields[i];

            summary.Add(new DashboardSummaryFieldDto
            {
                Order = i + 1,
                Name = field.Name,
                Label = field.Label,
                Value = i < _revealedCount ? _frozenValues[i] : null
            });
        }

        return summary;
    }

    private static string GenerateSerialNo(int seed)
    {
        return $"SN-{DateTime.Now:yyyyMMdd}{seed % 1000:D3}";
    }

    private static string GenerateLotSuffix(int seed)
    {
        return $"{DateTime.Now:yyyy-MMdd}-{seed % 100:D2}";
    }

    private static double[] GenerateClinchingHeights(int seed)
    {
        return Enumerable.Range(0, 5)
            .Select(i => Math.Round(12.40 + (seed + i) % 15 * 0.01, 2))
            .ToArray();
    }

    private sealed record FieldTemplate(
        string Name,
        string Label,
        Func<int, object?> ValueGenerator);
}
