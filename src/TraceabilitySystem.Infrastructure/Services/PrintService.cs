using System;
using System.Drawing.Printing;
using System.IO;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Domain.Interfaces;
using Zebra.Sdk.Comm;
using QRCoder;
using System.Drawing;
using System.Drawing.Drawing2D;
using DrawingFont = System.Drawing.Font;
using DrawingImage = System.Drawing.Image;
using DrawingBrushes = System.Drawing.Brushes;
using DrawingPens = System.Drawing.Pens;
using DrawingColor = System.Drawing.Color;
using DrawingFontStyle = System.Drawing.FontStyle;
using TraceabilitySystem.Application.DTOs.StockIn;
using Mapster;

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
    private readonly IAppConfigRepository _configRepository;
    private readonly IStockInRepository _stockInRepository;

    public PrintService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<PrintService> logger,
        IPrinterService printerService, IAppConfigRepository configRepository,
        IStockInRepository stockInRepository
        )
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _printerService = printerService;
        _configRepository = configRepository;
        _stockInRepository = stockInRepository;
    }

    public async Task PrintClinchingLabelWithSdkAsync(string serialNumberCode, CancellationToken cancellationToken = default)
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.Today);

        string dateFormat = today.ToString("yyyyMMdd");
        string mitsubishiCode = "T A000041130";
        string trssCode = "A T011000004";
        string qrCodeString = mitsubishiCode + ";" + trssCode + ";" + serialNumberCode;
        string printerName = await _configRepository.GetPrinterNameClinching(cancellationToken);


        using var scope = _serviceScopeFactory.CreateScope();
        var serialNumberRepository = scope.ServiceProvider.GetRequiredService<ISerialNumberRepository>();

        var serialNumberCheck = await serialNumberRepository.CheckByCodeAsync(serialNumberCode, cancellationToken);
        if(serialNumberCheck == false)
        {
            _logger.LogWarning("Serial number [{SerialNumberCode}] not found. Skipping print job.", serialNumberCode);
            return;
        }

        var zpl = BuildZplLabelClinching(mitsubishiCode,trssCode,serialNumberCode,dateFormat, qrCodeString);

        await SendViaZebraSdkAsync(printerName, zpl);

    }


    private static string BuildZplLabelClinching(
        string mitsubishiCode,
        string trssCode,
        string serialNumberCode,
        string dateFormat,
        string qrCodeString)
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

            ^FO360,75^BQN,3,3^FDQA,{qrCodeString}^FS

            ^FO65,16^A0N,30,50^FD{mitsubishiCode}^FS
            ^FO65,42^A0N,30,50^FD{trssCode}^FS

            ^FO20,81^A0N,22,22^FD{serialNumberCode}^FS

            ^FO20,114^A0N,22,22^FD{dateFormat}^FS
            ^FO170,115^A0N,15,15^FDMADE IN INDONESIA^FS

            ^XZ
        """;
    }


    private Task SendViaZebraSdkAsync(string printerName, string zplData)
    {
        return Task.Run(() =>
        {
            Connection connection = new DriverPrinterConnection(printerName);

            try
            {
                _logger.LogInformation("Connecting to Zebra printer '{PrinterName}'...", printerName);

                connection.Open();

                byte[] bytes = Encoding.UTF8.GetBytes(zplData);

                connection.Write(bytes);

                _logger.LogInformation("Print completed successfully.");
            }
            catch (ConnectionException ex)
            {
                _logger.LogError(ex, "Failed to connect to printer '{PrinterName}'", printerName);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while printing.");
                throw;
            }
            finally
            {
                connection.Close();
            }
        });
    }

    
    public async Task PrintClinchingShortSideAsync(string serialNumberCode, CancellationToken cancellationToken = default)
    {
        try
        {
            await PrintClinchingLabelWithSdkAsync(serialNumberCode, cancellationToken);
        }catch(Exception ex)
        {
            _logger.LogError(ex, "Error printing....");
            throw;
        }
    }

    public async Task PrintStockInAsync(StockInDto stockInDto, CancellationToken cancellationToken = default)
    {
        await PrintStockInProcessAsync(stockInDto, cancellationToken);
    }

    private async Task PrintStockInProcessAsync(StockInDto stockInDto, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Printing stock in processing....");

        string printerNameStockIn = await _configRepository.GetPrinterNameStockIn(cancellationToken);

        var issue = stockInDto.Issues.First();
        var part = stockInDto.Part!;

        var issueNumber = issue.Number;
        var partNumber = part.Number;
        var partName = part.Name;
        var supplyQty = stockInDto.SupplyQty.ToString();
        var supplyDate = stockInDto.SupplyDate.ToString("yyyy.MM.dd");
        var receiptDate = stockInDto.ReceiptDate.ToString("yyyy.MM.dd");

        try
        {
            // Generate QR
            string qrContent = string.Join(";", new[]
            {
                issueNumber,
                partNumber,
                partName,
                supplyQty,
                supplyDate,
                receiptDate
            });

            using var qrGenerator = new QRCodeGenerator();
            using var qrData = qrGenerator.CreateQrCode(qrContent, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrData);
            var qrBytes = qrCode.GetGraphic(20);

            using var qrStream = new MemoryStream(qrBytes);
            using var qrImage = System.Drawing.Image.FromStream(qrStream);

            PrintDocument pd = new();

            pd.PrinterSettings.PrinterName = printerNameStockIn;

            pd.DefaultPageSettings.Landscape = true;

            // A5
            pd.DefaultPageSettings.PaperSize = new PaperSize("A5", 583, 827);

            pd.PrintPage += (sender, e) =>
            {
                Graphics g = e.Graphics;
                int pageWidth = e.PageBounds.Width;
                int pageHeight = e.PageBounds.Height;

                g.Clear(DrawingColor.White);
                g.SmoothingMode = SmoothingMode.HighQuality;

                using DrawingFont labelFont = new("Arial", 12, DrawingFontStyle.Bold);
                using DrawingFont valueFont = new("Arial", 18, DrawingFontStyle.Bold);

                Pen pen = Pens.Black;

                // Margin lebih besar
                int marginHorizontal = 80;
                int marginVertical = 80;

                int printableWidth = pageWidth - (marginHorizontal * 2);
                int printableHeight = pageHeight - (marginVertical * 2);

                // Tinggi tiap row
                int rowHeight = printableHeight / 6;

                // Lebar kolom
                int labelWidth = (int)(printableWidth * 0.32);
                int qrWidth = (int)(printableWidth * 0.30);
                int valueWidth = printableWidth - labelWidth - qrWidth;

                int startX = marginHorizontal;
                int startY = marginVertical;

                StringFormat leftMiddle = new()
                {
                    Alignment = StringAlignment.Near,
                    LineAlignment = StringAlignment.Center
                };

                string[] labels =
                {
                "Issue No / 発行No.",
                "Parts No / 品番",
                "Parts Name / 品名",
                "Supply Qty / 供給数",
                "Supply Date / 供給日",
                "Receipt Date / 入荷日"
            };

                string[] values =
                {
                    issueNumber,
                    partNumber,
                    partName,
                    supplyQty,
                    supplyDate,
                    receiptDate
                };

                for (int i = 0; i < labels.Length; i++)
                {
                    int y = startY + (i * rowHeight);

                    // Label
                    g.DrawRectangle(
                        pen,
                        startX,
                        y,
                        labelWidth,
                        rowHeight);

                    // Value
                    g.DrawRectangle(
                        pen,
                        startX + labelWidth,
                        y,
                        valueWidth,
                        rowHeight);

                    g.DrawString(
                     labels[i],
                     labelFont,
                     Brushes.Black,
                     new RectangleF(
                         startX + 12,
                         y,
                         labelWidth - 20,
                         rowHeight),
                     leftMiddle);

                    g.DrawString(
                        values[i],
                        valueFont,
                        Brushes.Black,
                        new RectangleF(
                            startX + labelWidth + 12,
                            y,
                            valueWidth - 20,
                            rowHeight),
                        leftMiddle);
                }

                // QR Area
                g.DrawRectangle(
                    pen,
                    startX + labelWidth + valueWidth,
                    startY,
                    qrWidth,
                    rowHeight * 6);

                int qrSize = 150;

                int qrX = startX + labelWidth + valueWidth + ((qrWidth - qrSize) / 2);

                int qrY = startY + ((rowHeight * 6 - qrSize) / 2);

                g.DrawImage(
                    qrImage,
                    qrX,
                    qrY,
                    qrSize,
                    qrSize);
            };

            pd.Print();

            _logger.LogInformation("Printing stock in completed....");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error printing stock in....");
            throw;
        }
    }

    //public async Task PrintMFanAssyAsync(string issueNumber, CancellationToken cancellationToken = default)
    //{
    //    await PrintClinchingLabelWithSdkAsync(issueNumber, cancellationToken);
    //}
}
