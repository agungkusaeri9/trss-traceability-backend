using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TraceabilitySystem.Application.DTOs.StockIn;
using TraceabilitySystem.Application.Interfaces;
using Zebra.Sdk.Comm;

namespace TraceabilitySystem.Infrastructure.Services;

/// <summary>
/// Sends ZPL (Zebra Printer Language) label data to a network printer
/// via raw TCP socket connection on the configured IP and port.
///
/// Label size  : 5.8 cm × 2.3 cm  @ 203 dpi  → 464 × 184 dots
/// </summary>
public class PrintService : IPrintService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<PrintService> _logger;
    private readonly IPrinterService _printerService;

    public PrintService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<PrintService> logger,
        IPrinterService printerService)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _printerService = printerService;
    }

    public async Task PrintStockInLabelAsync(StockInDto stockIn, CancellationToken cancellationToken = default)
    {
        var printer = await _printerService.GetStockInPrinterAsync(cancellationToken);
        if (printer is null)
        {
            _logger.LogWarning("StockIn printer not found. Skipping print job.");
            return;
        }

        var issueNumber = stockIn.Issues.Count > 0 ? stockIn.Issues[0].Number : "-";
        var partNumber = stockIn.Part?.Number ?? "-";
        var partName = stockIn.Part?.Name ?? "-";

        var zpl = BuildZplLabelStockIn(
            stockIn.Code, issueNumber, partNumber, partName,
            stockIn.SupplyQty, stockIn.SupplyDate);

        // Use printer IP and Port from database
        await SendRawTcpAsync(printer.IpAddress, printer.Port, zpl, cancellationToken);

        _logger.LogInformation(
            "Print job sent for StockIn [{Code}] to printer {Name} at {Ip}:{Port}.",
            stockIn.Code, printer.Name, printer.IpAddress, printer.Port);
    }

    /// <summary>
    /// Prints using Zebra SDK (Zebra Link-OS SDK for .NET)
    /// This method uses the official Zebra SDK instead of raw TCP socket.
    /// Requires Zebra.Printer.SDK NuGet package.
    /// </summary>
    public async Task PrintStockInLabelWithSdkAsync(StockInDto stockIn, CancellationToken cancellationToken = default)
    {
        var printer = await _printerService.GetStockInPrinterAsync(cancellationToken);
        if (printer is null)
        {
            _logger.LogWarning("StockIn printer not found. Skipping print job.");
            return;
        }

        var issueNumber = stockIn.Issues.Count > 0 ? stockIn.Issues[0].Number : "-";
        var partNumber = stockIn.Part?.Number ?? "-";
        var partName = stockIn.Part?.Name ?? "-";

        var zpl = BuildZplLabelStockIn(
            stockIn.Code, issueNumber, partNumber, partName,
            stockIn.SupplyQty, stockIn.SupplyDate);

        // Use printer IP and Port from database
        await SendViaZebraSdkAsync(printer.IpAddress, printer.Port, zpl);

        _logger.LogInformation(
            "Print job sent (SDK) for StockIn [{Code}] to printer {Name} at {Ip}:{Port}.",
            stockIn.Code, printer.Name, printer.IpAddress, printer.Port);
    }

    /// <summary>
    /// Get raw ZPL string for a StockIn label
    /// </summary>
    public string GetZplForStockIn(StockInDto stockIn)
    {
        var issueNumber = stockIn.Issues.Count > 0 ? stockIn.Issues[0].Number : "-";
        var partNumber = stockIn.Part?.Number ?? "-";
        var partName = stockIn.Part?.Name ?? "-";

        return BuildZplLabelStockIn(
            stockIn.Code, issueNumber, partNumber, partName,
            stockIn.SupplyQty, stockIn.SupplyDate);
    }

    /// <summary>
    /// Get the configured StockIn printer from PrinterService
    /// </summary>
    // private async Task<Domain.Entities.Printer?> GetStockInPrinterAsync(CancellationToken cancellationToken)
    // {
    //     // using var scope = _serviceScopeFactory.CreateScope();
    //     // var printerService = scope.ServiceProvider.GetRequiredService<IPrinterService>();
    //     return await printerService.GetStockInPrinterAsync(cancellationToken);
    // }

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

        return $"""
            ^XA
            ^POI
            ^MD28
            ^PR2
            ^PW{labelW}
            ^LL{labelH}
            ^CI28

            ^FO330,75^BQN,3,3^FDQA,{code}^FS

            ^FO40,15^A0N,30,50^FDY {code}^FS

            ^FO40,40^A0N,30,50^FDA {partNumber}^FS

            ^FO20,75^A0N,22,22^FDPCS{qty:D5}^FS

            ^FO20,110^A0N,22,22^FD{supplyDate:ddMMyyyy}^FS
            ^FO170,115^A0N,15,15^FDMADE IN INDONESIA^FS


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

    /// <summary>
    /// Sends ZPL data to printer using Zebra SDK TcpConnection.
    /// This provides better error handling and connection management than raw TCP.
    /// </summary>
    private Task SendViaZebraSdkAsync(string ip, int port, string zplData)
    {
        return Task.Run(() =>
        {
            var connection = new TcpConnection(ip, port);

            try
            {
                _logger.LogInformation("Connecting to Zebra printer at {Ip}:{Port}...", ip, port);
                connection.Open();

                var bytes = Encoding.UTF8.GetBytes(zplData);
                connection.Write(bytes);

                _logger.LogInformation("Print completed successfully.");
            }
            catch (ConnectionException ex)
            {
                _logger.LogError(ex, "Connection error with Zebra printer at {Ip}:{Port}", ip, port);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error when printing to Zebra printer");
                throw;
            }
            finally
            {
                connection.Close();
            }
        });
    }
}
