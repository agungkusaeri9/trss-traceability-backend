using System.Threading;
using System.Threading.Tasks;
using TraceabilitySystem.Application.DTOs.StockIn;

namespace TraceabilitySystem.Application.Interfaces;

public interface IPrintService
{
    /// <summary>
    /// Sends a label print job to the configured printer for a stock-in record.
    /// Uses Zebra SDK.
    /// </summary>
    //Task PrintClinchingLabelWithSdkAsync(string issueNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get raw ZPL string for a StockIn label
    /// </summary>
    //string GetZplForStockIn(StockInDto stockIn);

    /// <summary>
    /// Generate PDF for StockIn label (A5 landscape)
    /// </summary>
    //byte[] GeneratePdfForStockIn(StockInDto stockIn);

    /// <summary>
    /// Process print for CLINCHING_SHORT_SIDE process code
    /// </summary>
    Task PrintClinchingShortSideAsync(string serialNumber, CancellationToken cancellationToken = default);
    Task PrintStockInAsync(StockInDto stockInDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Process print for M_FAN_ASSY process code
    /// </summary>
    //Task PrintMFanAssyAsync(string issueNumber, CancellationToken cancellationToken = default);
}
