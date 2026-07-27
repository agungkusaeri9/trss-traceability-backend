using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.IO;
using System.Linq.Expressions;
using System.Net.Sockets;
using System.Text;
using Mapster;
using Microsoft.Extensions.Logging;
using QRCoder;
using TraceabilitySystem.Application.DTOs.PrintHistory;
using TraceabilitySystem.Application.DTOs.StockIn;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Domain.Entities;
using TraceabilitySystem.Domain.Enums;
using TraceabilitySystem.Domain.Interfaces;
using TraceabilitySystem.Infrastructure.Persistence.Repositories;
using TraceabilitySystem.Shared.Exceptions;
using TraceabilitySystem.Shared.Models;
using Zebra.Sdk.Comm;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using DrawingBrushes = System.Drawing.Brushes;
using DrawingColor = System.Drawing.Color;
using DrawingFont = System.Drawing.Font;
using DrawingFontStyle = System.Drawing.FontStyle;
using DrawingImage = System.Drawing.Image;
using DrawingPens = System.Drawing.Pens;
using System.Management;

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
    private readonly IPrintHistoryService _printHistoryService;
    private readonly IStockInRepository _stockInRepository;
    private readonly IPrintHistoryRepository _printHistoryRepository;
    private readonly ISerialNumberRepository _serialNumberRepo;
    private readonly IMqttPublisher _mqttPublisher;

    public PrintService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<PrintService> logger,
        IPrinterService printerService, IAppConfigRepository configRepository,
        IStockInRepository stockInRepository,
        IPrintHistoryService printHistoryService,
        IPrintHistoryRepository printHistoryRepository,
        ISerialNumberRepository serialNumberRepo,
        IMqttPublisher mqttPublisher
        )
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _printerService = printerService;
        _configRepository = configRepository;
        _stockInRepository = stockInRepository;
        _printHistoryService = printHistoryService;
        _printHistoryRepository = printHistoryRepository;
        _serialNumberRepo = serialNumberRepo;
        _mqttPublisher = mqttPublisher;
    }

    
    private async Task PrintClinchingLabelWithSdkAsync(string serialNumberCode, CancellationToken cancellationToken = default)
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
            Zebra.Sdk.Comm.Connection connection = new DriverPrinterConnection(printerName);

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

    
    public async Task PrintClinchingShortSideAsync(string serialNumberCode, List<string>? issueNumbers = null, CancellationToken cancellationToken = default)
    {
        var printHistoryDto = new PrintHistoryCreateClinchingDto
        {
            Status = PrintStatus.Success,
            SerialNumberCode = serialNumberCode,
        };

        try
        {
            await PrintClinchingLabelWithSdkAsync(serialNumberCode, cancellationToken);
            await _printHistoryService.CreateHistoryPrintClinchingAsync(printHistoryDto);

            await _mqttPublisher.PublishAsync("data/print/clinching-short-side", new
            {
                IsPrinted = true,
                SerialNumber = serialNumberCode,
                IssueNumbers = issueNumbers ?? new List<string>()
            }, cancellationToken);
        }catch(Exception ex)
        {
            printHistoryDto.Status = PrintStatus.Failed;
            printHistoryDto.ErrorMessage = ex.Message;
            await _printHistoryService.CreateHistoryPrintClinchingAsync(printHistoryDto);

            await _mqttPublisher.PublishAsync("data/print/clinching-short-side", new
            {
                IsPrinted = false,
                SerialNumber = serialNumberCode,
                IssueNumbers = issueNumbers ?? new List<string>()
            }, cancellationToken);
        }
    }

    public async Task PrintStockInAsync(StockInDto stockInDto, CancellationToken cancellationToken = default)
    {
        var printHistoryDto = new PrintHistoryCreateStockInDto
        {
            Status = PrintStatus.Success,
            IssueNumber = stockInDto!.Issues!.FirstOrDefault()!.Number,
        };

        try
        {
            await PrintStockInProcessAsync(stockInDto, cancellationToken);
            await _printHistoryService.CreateHistoryPrintStockInAsync(printHistoryDto);
        }
        catch (Exception ex)
        {
            printHistoryDto.Status = PrintStatus.Failed;
            printHistoryDto.ErrorMessage = ex.Message;
            await _printHistoryService.CreateHistoryPrintStockInAsync(printHistoryDto);

        }
    }

    private static void ValidatePrinter(string printerName)
    {
        using var searcher = new ManagementObjectSearcher(
            $"SELECT * FROM Win32_Printer WHERE Name='{printerName.Replace("\\", "\\\\")}'");

        var printer = searcher.Get()
            .Cast<ManagementObject>()
            .FirstOrDefault();

        if (printer == null)
            throw new InvalidOperationException($"Printer '{printerName}' not found.");

        if ((bool)printer["WorkOffline"])
            throw new InvalidOperationException($"Printer '{printerName}' is offline.");
    }

    private async Task PrintStockInProcessAsync(StockInDto stockInDto, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Printing stock in processing....");
        string printerNameStockIn = await _configRepository.GetPrinterNameStockIn(cancellationToken);

        var issue = stockInDto.Issues.First();
        var part = stockInDto.Part!;
        var issueNumber = issue.Number;
        var specialIssueNumber = (part.SpecialCharacter ?? string.Empty) + issue.Number;
        var partNumber = part.Number + part.SpecialCharacter;
        var partName = part.Name;
        var supplyQty = stockInDto.SupplyQty.ToString();
        var supplyDate = stockInDto.SupplyDate.ToString("yyyy.MM.dd");
        var receiptDate = stockInDto.ReceiptDate.ToString("yyyy.MM.dd");

        // Generate QR
        string qrContent = string.Join(";", new[]
        {
                specialIssueNumber,
                partNumber,
                partName,
                supplyQty,
                supplyDate,
                receiptDate
            });
        try
        {


            using var qrGenerator = new QRCodeGenerator();
            using var qrData = qrGenerator.CreateQrCode(qrContent, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrData);
            var qrBytes = qrCode.GetGraphic(20);

            using var qrStream = new MemoryStream(qrBytes);
            using var qrImage = System.Drawing.Image.FromStream(qrStream);

            PrintDocument pd = new();

            pd.PrinterSettings.PrinterName = printerNameStockIn;

            if (!pd.PrinterSettings.IsValid)
            {
                throw new InvalidOperationException(
                    $"Printer '{printerNameStockIn}' was not found.");
            }

            ValidatePrinter(printerNameStockIn);


            pd.PrinterSettings.PrinterName = printerNameStockIn;


            pd.DefaultPageSettings.Landscape = true;

            // A5
            pd.DefaultPageSettings.PaperSize = new PaperSize("A5", 583, 827);

            pd.PrintPage += (sender, e) =>
            {
                Graphics g = e.Graphics!;
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
        }
        catch (ConnectionException ex)
        {
            _logger.LogError(ex, "Failed to connect to printer '{printerNameStockIn}'", printerNameStockIn);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while printing.");
            throw;
        }
    }

    //public async Task PrintMFanAssyAsync(string issueNumber, CancellationToken cancellationToken = default)
    //{
    //    await PrintClinchingLabelWithSdkAsync(issueNumber, cancellationToken);
    //}

    public async Task RePrintAsync(int id, CancellationToken cancellation = default)
    {
        var result = await _printHistoryRepository.GetByIdAsync(id);
        if (result is null)
        {
            throw new NotFoundException("Print history not found",nameof(id));
        }
        try
        {

            if (result.Module == PrintModule.StockIn)
                await RePrintStockInAsync(result.ReferenceNumber!);
            else if (result.Module == PrintModule.Clinching)
                await RePrintClinchingAsync(result.ReferenceNumber!);


        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MASUK REPRINT CATCH");


            result.RetryCount += 1;
            result.LastRetryAt = DateTime.UtcNow;
            _printHistoryRepository.Update(result);
            await _printHistoryRepository.SaveChangesAsync();
            throw new AppException($"Reprint failed: {ex.Message}");
        }
    }

    private async Task RePrintStockInAsync(string issueNumber)
    {
        try
        {
            var stockIn = await _stockInRepository.GetByIssueNumberAsync(issueNumber);
            if (stockIn is null)
                throw new KeyNotFoundException("Stock In not found.");
            var stockInDto = stockIn.Adapt<StockInDto>();
            await PrintStockInProcessAsync(stockInDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during reprint stock in for issue number: {IssueNumber}", issueNumber);
            throw;
        }
    }

    private async Task RePrintClinchingAsync(string serialNumberCode, CancellationToken cancellationToken = default)
    {
        var serialNumber = await _serialNumberRepo.GetWithRelatedBySerialNumberAsync(serialNumberCode);
        if (serialNumber is null)
            throw new KeyNotFoundException("Serial Number not found.");
        await PrintClinchingLabelWithSdkAsync(serialNumber.SerialNumberCode, cancellationToken);
    }
}
