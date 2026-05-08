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
///
/// Label size  : 5.8 cm × 2.3 cm  @ 203 dpi  → 464 × 184 dots
/// </summary>
public class PrintService : IPrintService
{
    private const string StockInPrinterIp   = "192.168.245.248";
    private const int    StockInPrinterPort = 9100;

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
        var partNumber  = stockIn.Part?.Number ?? "-";
        var partName    = stockIn.Part?.Name   ?? "-";

        var zpl = BuildZplLabelStockIn(
            stockIn.Code, issueNumber, partNumber, partName,
            stockIn.SupplyQty, stockIn.SupplyDate);

        await SendRawTcpAsync(StockInPrinterIp, StockInPrinterPort, zpl, cancellationToken);

        _logger.LogInformation(
            "Print job sent for StockIn [{Code}] to StockIn printer at {Ip}:{Port}.",
            stockIn.Code, StockInPrinterIp, StockInPrinterPort);

        PreviewZplToConsole(stockIn.Code, issueNumber, partNumber, partName,
                            stockIn.SupplyQty, stockIn.SupplyDate, zpl);
    }

    private static string BuildZplLabelStockIn(
        string code,
        string issueNumber,
        string partNumber,
        string partName,
        int qty,
        DateTime supplyDate)
    {
        const int labelW = 464;
        const int labelH = 184;

        // ^MD30 sets darkness (0-30), ^PR2 sets print speed (2 inches/sec)
        return $"""
            ^XA
            ^POI
            ^MD30
            ^PR2
            ^PW{labelW}
            ^LL{labelH}
            ^CI28

            ^FO350,10^BQN,2,3^FDQA,{code}^FS

            ^FO20,15^A0N,22,22^FDY {code}^FS
            ^FO20,40^A0N,22,22^FDA {partNumber}^FS

            ^FO350,100^A0N,22,22^FDPCS{qty:D5}^FS
            ^FO350,130^A0N,22,22^FD{supplyDate:ddMMyyyy}^FS

            ^FO20,130^A0N,15,15^FDMADE IN INDONESIA^FS

            ^FO350,155^GB25,25,1,B,10^FS
            ^FO355,161^A0N,12,12^FDTRSS^FS

            ^XZ
            """;
    }

    private static void PreviewZplToConsole(
        string code, string issueNumber, string partNumber,
        string partName, int qty, DateTime supplyDate, string rawZpl)
    {
        const int W = 62;
        var border = new string('=', W);

        Console.WriteLine();
        Console.WriteLine(border);
        Console.WriteLine($" [QR: {code,-10}]   Y {code}");
        Console.WriteLine($"                  A {partNumber}");
        Console.WriteLine();
        Console.WriteLine($" PCS{qty:D5}");
        Console.WriteLine($" {supplyDate:ddMMyyyy}           MADE IN INDONESIA");
        Console.WriteLine();
        Console.WriteLine(" [TRSS]");
        Console.WriteLine(border);

        Console.WriteLine();
        Console.WriteLine("── RAW ZPL ──────────────────────────────────────────────────");
        Console.WriteLine(rawZpl);
        Console.WriteLine("─────────────────────────────────────────────────────────────");
        Console.WriteLine($"► Sending to {StockInPrinterIp}:{StockInPrinterPort}");
        Console.WriteLine();
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
