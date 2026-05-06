using System.Threading;
using System.Threading.Tasks;
using TraceabilitySystem.Application.DTOs.StockIn;

namespace TraceabilitySystem.Application.Interfaces;

public interface IPrintService
{
    /// <summary>
    /// Sends a label print job to the configured printer for a stock-in record.
    /// The printer is resolved by its ID from the database.
    /// </summary>
    Task PrintStockInLabelAsync(StockInDto stockIn, int printerId, CancellationToken cancellationToken = default);
}
