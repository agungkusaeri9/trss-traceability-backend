using Microsoft.Extensions.Logging;
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TraceabilitySystem.Application.DTOs.StockIn;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Domain.Interfaces;

namespace TraceabilitySystem.Infrastructure.Services;

/// <summary>
/// Sends ZPL (Zebra Printer Language) label data to a network printer
/// via raw TCP socket connection on the configured IP and port.
/// </summary>
public class PrintService : IPrintService
{
    private readonly IPrinterRepository _printerRepository;
    private readonly ILogger<PrintService> _logger;

    public PrintService(IPrinterRepository printerRepository, ILogger<PrintService> logger)
    {
        _printerRepository = printerRepository;
        _logger = logger;
    }

    public async Task PrintStockInLabelAsync(StockInDto stockIn, int printerId, CancellationToken cancellationToken = default)
    {
        var printer = await _printerRepository.GetByIdAsync(printerId, cancellationToken);
        if (printer is null || !printer.IsActive)
        {
            _logger.LogWarning("Printer with ID {PrinterId} not found or inactive. Skipping print job.", printerId);
            return;
        }

        var issueNumber = stockIn.Issues.Count > 0 ? stockIn.Issues[0].Number : "-";
        var partNumber = stockIn.Part?.Number ?? "-";
        var partName = stockIn.Part?.Name ?? "-";

        // Build ZPL label — adjust template to match your actual label format
        var zpl = BuildZplLabel(stockIn.Code, issueNumber, partNumber, partName, stockIn.SupplyQty, stockIn.SupplyDate);

        await SendRawTcpAsync(printer.IpAddress, printer.Port, zpl, cancellationToken);

        _logger.LogInformation(
            "Print job sent for StockIn [{Code}] to printer [{PrinterName}] at {Ip}:{Port}.",
            stockIn.Code, printer.Name, printer.IpAddress, printer.Port);
    }

    private static string BuildZplLabel(
        string code,
        string issueNumber,
        string partNumber,
        string partName,
        int qty,
        DateTime supplyDate)
    {
        // ZPL template — 4" x 2" label @ 203 dpi
        return $"""
            ^XA
            ^FO30,20^A0N,28,28^FDTreasability System^FS
            ^FO30,55^A0N,22,22^FDCode: {code}^FS
            ^FO30,85^A0N,22,22^FDIssue: {issueNumber}^FS
            ^FO30,115^A0N,22,22^FDPart: {partNumber}^FS
            ^FO30,145^A0N,22,22^FD{partName}^FS
            ^FO30,175^A0N,22,22^FDQty: {qty}^FS
            ^FO30,205^A0N,22,22^FDDate: {supplyDate:yyyy-MM-dd}^FS
            ^FO30,240^BY2^BCN,60,Y,N,N^FD{code}^FS
            ^XZ
            """;
    }

    private static async Task SendRawTcpAsync(string ip, int port, string data, CancellationToken cancellationToken)
    {
        using var client = new TcpClient();
        await client.ConnectAsync(ip, port, cancellationToken);

        var bytes = Encoding.UTF8.GetBytes(data);
        var stream = client.GetStream();
        await stream.WriteAsync(bytes, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }
}
