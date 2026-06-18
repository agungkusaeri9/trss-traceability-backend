using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TraceabilitySystem.Application.DTOs.StockIn;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Domain.Entities;
using TraceabilitySystem.Domain.Interfaces;
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

    // public async Task PrintClinchingLabelAsync(StockInDto stockIn, CancellationToken cancellationToken = default)
    // {
    //     var printer = await _printerService.GetClinchingPrinterAsync(cancellationToken);
    //     if (printer is null)
    //     {
    //         _logger.LogWarning("Clinching printer not found. Skipping print job.");
    //         return;
    //     }

    //     var issueNumber = stockIn.Issues.Count > 0 ? stockIn.Issues[0].Number : "-";
    //     var partNumber = stockIn.Part?.Number ?? "-";
    //     var partName = stockIn.Part?.Name ?? "-";

    //     var zpl = BuildZplLabelStockIn(
    //         stockIn.Code, issueNumber, partNumber, partName,
    //         stockIn.SupplyQty, stockIn.SupplyDate);

    //     // Use printer IP and Port from database
    //     await SendRawTcpAsync(printer.IpAddress, printer.Port, zpl, cancellationToken);

    //     _logger.LogInformation(
    //         "Print job sent for StockIn [{Code}] to printer {Name} at {Ip}:{Port}.",
    //         stockIn.Code, printer.Name, printer.IpAddress, printer.Port);
    // }

    /// <summary>
    /// Prints using Zebra SDK (Zebra Link-OS SDK for .NET)
    /// This method uses the official Zebra SDK instead of raw TCP socket.
    /// Requires Zebra.Printer.SDK NuGet package.
    /// </summary>
    public async Task PrintClinchingLabelWithSdkAsync(string issueNumber, CancellationToken cancellationToken = default)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var issueRepository = scope.ServiceProvider.GetRequiredService<IIssueRepository>();

        var issue = await issueRepository
            .FirstOrDefaultAsync(i => i.Number == issueNumber, cancellationToken);

        if (issue == null)
        {
            _logger.LogWarning("Issue with number {IssueNumber} not found. Skipping print job.", issueNumber);
            return;
        }

        var stockInRepository = scope.ServiceProvider.GetRequiredService<IStockInRepository>();
        var stockIn = await stockInRepository
            .GetByIdAsync(issue.StockInId, cancellationToken);

        if (stockIn == null)
        {
            _logger.LogWarning("StockIn with id {StockInId} not found. Skipping print job.", issue.StockInId);
            return;
        }

        var printer = await _printerService.GetStockInPrinterAsync(cancellationToken);
        if (printer is null)
        {
            _logger.LogWarning("StockIn printer not found. Skipping print job.");
            return;
        }

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
            ^MD25
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

    /// <summary>
    /// Generate PDF for StockIn label using QuestPDF (A5 landscape)
    /// </summary>
    public byte[] GeneratePdfForStockIn(StockInDto stockIn)
    {
        // Configure QuestPDF license
        QuestPDF.Settings.License = LicenseType.Community;

        var issueNumber = stockIn.Issues.Count > 0 ? stockIn.Issues[0].Number : "-";
        var partNumber = stockIn.Part?.Number ?? "-";
        var partName = stockIn.Part?.Name ?? "-";

        // A5 landscape dimensions in mm: 210mm x 148.5mm
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5.Landscape());
                page.Margin(5, Unit.Millimetre);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Content().Column(column =>
                {
                    column.Spacing(3);

                    // Title
                    column.Item().Text("２．Issue Label / ラベル発行")
                        .FontSize(14).Bold();

                    // Row 1: Issue No
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Border(1).Padding(3).Text(td => td.Span("Issue No / 発行No.").Bold());
                        row.RelativeItem(2).Border(1).Padding(3).Text(issueNumber).FontSize(16).Bold();
                        row.RelativeItem().Border(1).Padding(3).AlignCenter().Text(td =>
                        {
                            td.Line("QR CODE").FontSize(8);
                            td.Line($"[{issueNumber}]").FontSize(6);
                        });
                    });

                    // Row 2: Parts No
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Border(1).Padding(3).Text(td => td.Span("Parts No / 品番").Bold());
                        row.RelativeItem(2).Border(1).Padding(3).Text(partNumber).FontSize(14).Bold();
                        row.RelativeItem().Border(1).Padding(3);
                    });

                    // Row 3: Parts Name
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Border(1).Padding(3).Text(td => td.Span("Parts Name / 品名").Bold());
                        row.RelativeItem(2).Border(1).Padding(3).Text(partName).FontSize(14).Bold();
                        row.RelativeItem().Border(1).Padding(3);
                    });

                    // Row 4: Supply Qty
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Border(1).Padding(3).Text(td => td.Span("Supply Qty / 供給数").Bold());
                        row.RelativeItem(2).Border(1).Padding(3).Text(stockIn.SupplyQty.ToString()).FontSize(18).Bold();
                        row.RelativeItem().Border(1).Padding(3);
                    });

                    // Row 5: Supply Date
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Border(1).Padding(3).Text(td => td.Span("Supply Date / 供給日").Bold());
                        row.RelativeItem(2).Border(1).Padding(3).Text(stockIn.SupplyDate.ToString("yyyy.MM.dd")).FontSize(14).Bold();
                        row.RelativeItem().Border(1).Padding(3);
                    });

                    // Row 6: Receipt Date
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Border(1).Padding(3).Text(td => td.Span("Receipt Date / <minimax:tool_call>日").Bold());
                        row.RelativeItem(2).Border(1).Padding(3).Text(stockIn.ReceiptDate.ToString("yyyy.MM.dd")).FontSize(14).Bold();
                        row.RelativeItem().Border(1).Padding(3);
                    });
                });
            });
        });

        using var stream = new MemoryStream();
        document.GeneratePdf(stream);
        return stream.ToArray();
    }

    public async Task PrintClinchingShortSideAsync(string issueNumber, CancellationToken cancellationToken = default)
    {
        await PrintClinchingLabelWithSdkAsync(issueNumber, cancellationToken);
    }

    public async Task PrintMFanAssyAsync(string issueNumber, CancellationToken cancellationToken = default)
    {
        await PrintClinchingLabelWithSdkAsync(issueNumber, cancellationToken);
    }
}
