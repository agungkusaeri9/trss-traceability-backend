using System.Threading;
using System.Threading.Tasks;
using TraceabilitySystem.Application.DTOs.StockIn;

namespace TraceabilitySystem.Application.Interfaces;

public interface IPrintService
{
    /// <summary>
    /// Sends a label print job to the configured printer for a stock-in record.
    /// Uses raw TCP socket connection.
    /// </summary>
    Task PrintStockInLabelAsync(StockInDto stockIn, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a label print job using Zebra SDK (Zebra Link-OS SDK for .NET).
    /// Requires Zebra.Printer.SDK NuGet package.
    /// </summary>
    Task PrintStockInLabelWithSdkAsync(StockInDto stockIn, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get raw ZPL string for a StockIn label
    /// </summary>
    string GetZplForStockIn(StockInDto stockIn);
}
