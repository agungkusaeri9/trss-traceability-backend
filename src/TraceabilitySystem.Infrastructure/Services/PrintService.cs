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

        string dateFormat = today.ToString("ddMMyyyy");
        string mitsubishiCode = "21400C000P";
        string trssCode = "BM57100000";
        string qrCodeString = mitsubishiCode + " " + trssCode + " " + dateFormat + " " + serialNumberCode;
        
        string printerIp = await _configRepository.GetPrinterClinchingIpAsync(cancellationToken);
        int printerPort = await _configRepository.GetPrinterClinchingPortAsync(cancellationToken);

        using var scope = _serviceScopeFactory.CreateScope();
        var serialNumberRepository = scope.ServiceProvider.GetRequiredService<ISerialNumberRepository>();

        var serialNumberCheck = await serialNumberRepository.CheckByCodeAsync(serialNumberCode, cancellationToken);
        if (serialNumberCheck == false)
        {
            _logger.LogWarning("Serial number [{SerialNumberCode}] not found. Skipping print job.", serialNumberCode);
            throw new KeyNotFoundException($"Serial number [{serialNumberCode}] not found.");
        }

        var zpl = BuildZplLabelClinching(mitsubishiCode, trssCode, serialNumberCode, dateFormat, qrCodeString);

        await SendViaTcpAsync(printerIp, printerPort, zpl);
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

        ^FO20,16^A0N,32,60^FB424,1,0,C^FD{mitsubishiCode}^FS
        ^FO20,42^A0N,32,60^FB424,1,0,C^FD{trssCode}^FS

        ^FO20,81^A0N,22,22^FD{serialNumberCode}^FS

        ^FO20,114^A0N,22,22^FD{dateFormat}^FS
        ^FO170,115^A0N,17,17^FDMADE IN INDONESIA^FS

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
                _logger.LogError("Failed to connect to printer '{PrinterName}': {Message}", printerName, ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError("Unexpected error while printing to '{PrinterName}': {Message}", printerName, ex.Message);
                throw;
            }
            finally
            {
                connection.Close();
            }
        });
    }

    private Task SendViaTcpAsync(string ipAddress, int port, string zplData)
    {
        return Task.Run(() =>
        {
            // Set maxTimeoutForRead=3000ms, timeToWaitForMoreData=1000ms to avoid hanging
            Zebra.Sdk.Comm.Connection connection = new TcpConnection(ipAddress, port, 3000, 1000);

            try
            {
                _logger.LogInformation("Connecting to Zebra printer via TCP/IP '{IpAddress}:{Port}'...", ipAddress, port);

                connection.Open();

                _logger.LogInformation("Verifying Zebra printer status at '{IpAddress}:{Port}'...", ipAddress, port);
                Zebra.Sdk.Printer.ZebraPrinter printer = Zebra.Sdk.Printer.ZebraPrinterFactory.GetInstance(connection);
                Zebra.Sdk.Printer.PrinterStatus status = printer.GetCurrentStatus();

                if (!status.isReadyToPrint)
                {
                    if (status.isHeadOpen)
                        throw new InvalidOperationException($"Printer Zebra '{ipAddress}:{port}' error: Head is open.");
                    if (status.isPaperOut)
                        throw new InvalidOperationException($"Printer Zebra '{ipAddress}:{port}' error: Paper / Ribbon out.");
                    if (status.isPaused)
                        throw new InvalidOperationException($"Printer Zebra '{ipAddress}:{port}' error: Printer is paused.");

                    throw new InvalidOperationException($"Printer Zebra '{ipAddress}:{port}' is not ready to print (Status: Not Ready).");
                }

                byte[] bytes = Encoding.UTF8.GetBytes(zplData);

                connection.Write(bytes);

                _logger.LogInformation("Print via TCP/IP completed successfully.");
            }
            catch (ConnectionException ex)
            {
                _logger.LogError("Failed to connect/communicate with printer '{IpAddress}:{Port}': {Message}", ipAddress, port, ex.Message);
                throw;
            }
            catch (Zebra.Sdk.Printer.ZebraPrinterLanguageUnknownException ex)
            {
                _logger.LogError("Unknown printer language for printer '{IpAddress}:{Port}': {Message}", ipAddress, port, ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError("Unexpected error while printing to '{IpAddress}:{Port}': {Message}", ipAddress, port, ex.Message);
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

            await _mqttPublisher.PublishAsync("data/process/validation", new
            {
                status = true,
                process = "print-clinching",
                error = (string?)null,
                data = new
                {
                    serial_number = serialNumberCode
                }
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

            await _mqttPublisher.PublishAsync("data/process/validation", new
            {
                status = false,
                process = "print-clinching",
                error = ex.Message
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
        var partNumber = part.Number;
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
            pd.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);

            // A6 (105mm x 148mm -> 413 x 583)
            bool a6Found = false;
            foreach (PaperSize size in pd.PrinterSettings.PaperSizes)
            {
                if (size.PaperName.Equals("A6", StringComparison.OrdinalIgnoreCase) ||
                    size.RawKind == (int)PaperKind.A6)
                {
                    pd.DefaultPageSettings.PaperSize = size;
                    a6Found = true;
                    break;
                }
            }

            if (!a6Found)
            {
                pd.DefaultPageSettings.PaperSize = new PaperSize("A6", 413, 583);
            }

            pd.PrintPage += (sender, e) =>
            {
                Graphics g = e.Graphics!;
                int pageWidth = e.PageBounds.Width;
                int pageHeight = e.PageBounds.Height;

                g.Clear(DrawingColor.White);
                g.SmoothingMode = SmoothingMode.HighQuality;

                using DrawingFont labelFont = new("Arial", 9.5f, DrawingFontStyle.Bold);
                using DrawingFont valueFont = new("Arial", 13f, DrawingFontStyle.Bold);

                Pen pen = Pens.Black;

                // Margin minimal
                int marginHorizontal = 10;
                int marginVertical = 10;

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
                            startX + 6,
                            y,
                            labelWidth - 10,
                            rowHeight),
                        leftMiddle);

                    g.DrawString(
                        values[i],
                        valueFont,
                        Brushes.Black,
                        new RectangleF(
                            startX + labelWidth + 6,
                            y,
                            valueWidth - 10,
                            rowHeight),
                        leftMiddle);
                }

                // QR Area
                int totalTableHeight = rowHeight * 6;
                g.DrawRectangle(
                    pen,
                    startX + labelWidth + valueWidth,
                    startY,
                    qrWidth,
                    totalTableHeight);

                int qrSize = Math.Min(qrWidth - 16, totalTableHeight - 16);

                int qrX = startX + labelWidth + valueWidth + ((qrWidth - qrSize) / 2);
                int qrY = startY + ((totalTableHeight - qrSize) / 2);

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

    private async Task PrintMFanAssyLabelWithSdkAsync(string serialNumberCode, CancellationToken cancellationToken = default)
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.Today);

        string dateFormat = today.ToString("ddMMyyyy");
        string mitsubishiCode = "21400C000P";
        string trssCode = "BM57100000";
        string qrCodeString = mitsubishiCode + " " + trssCode + " " + dateFormat + " " + serialNumberCode;

        using var scope = _serviceScopeFactory.CreateScope();
        var serialNumberRepository = scope.ServiceProvider.GetRequiredService<ISerialNumberRepository>();

        var serialNumberCheck = await serialNumberRepository.CheckByCodeAsync(serialNumberCode, cancellationToken);
        if (serialNumberCheck == false)
        {
            _logger.LogWarning("Serial number [{SerialNumberCode}] not found. Skipping print job.", serialNumberCode);
            throw new KeyNotFoundException($"Serial number [{serialNumberCode}] not found.");
        }

        // Cek apakah mode test print ke printer Stock In aktif
        bool isTestMode = await _configRepository.GetIsTestModeMFanAssyAsync(cancellationToken);
        if (isTestMode)
        {
            _logger.LogInformation("[TEST PRINT] Mode test aktif. Mengirim print label M-Fan Assy ke printer Stock In untuk SN: {SerialNumberCode}", serialNumberCode);
            await PrintMFanAssyLabelTestWithStockInPrinterAsync(mitsubishiCode, trssCode, serialNumberCode, dateFormat, qrCodeString, cancellationToken);
            return;
        }

        // Mode normal / produksi: Kirim via TCP socket (ZPL)
        string printerIp = await _configRepository.GetPrinterMFanAssyIpAsync(cancellationToken);
        int printerPort = await _configRepository.GetPrinterMFanAssyPortAsync(cancellationToken);

        var zpl = BuildZplLabelClinching(mitsubishiCode, trssCode, serialNumberCode, dateFormat, qrCodeString);

        await SendViaTcpAsync(printerIp, printerPort, zpl);
    }

    /// <summary>
    /// Desain label versi test untuk dicetak pada printer Stock In (Windows Driver / GDI PrintDocument).
    /// </summary>
    private async Task PrintMFanAssyLabelTestWithStockInPrinterAsync(
        string mitsubishiCode,
        string trssCode,
        string serialNumberCode,
        string dateFormat,
        string qrCodeString,
        CancellationToken cancellationToken = default)
    {
        string printerNameStockIn = await _configRepository.GetPrinterNameStockIn(cancellationToken);

        if (string.IsNullOrWhiteSpace(printerNameStockIn))
        {
            throw new InvalidOperationException("PRINTER_NAME_STOCK_IN is not configured for test printing.");
        }

        try
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrData = qrGenerator.CreateQrCode(qrCodeString, QRCodeGenerator.ECCLevel.Q);
            var qrCode = new PngByteQRCode(qrData);
            var qrBytes = qrCode.GetGraphic(20);

            using var qrStream = new MemoryStream(qrBytes);
            using var qrImage = DrawingImage.FromStream(qrStream);

            PrintDocument pd = new();
            pd.PrinterSettings.PrinterName = printerNameStockIn;

            if (!pd.PrinterSettings.IsValid)
            {
                throw new InvalidOperationException($"Printer '{printerNameStockIn}' was not found.");
            }

            ValidatePrinter(printerNameStockIn);

            pd.DefaultPageSettings.Landscape = true;

            // Pastikan ukuran kertas A5 (148mm x 210mm)
            bool a5Found = false;
            foreach (PaperSize size in pd.PrinterSettings.PaperSizes)
            {
                if (size.PaperName.Equals("A5", StringComparison.OrdinalIgnoreCase))
                {
                    pd.DefaultPageSettings.PaperSize = size;
                    a5Found = true;
                    break;
                }
            }

            if (!a5Found)
            {
                pd.DefaultPageSettings.PaperSize = new PaperSize("A5", 583, 827);
            }

            pd.PrintPage += (sender, e) =>
            {
                Graphics g = e.Graphics!;
                int pageWidth = e.PageBounds.Width;
                int pageHeight = e.PageBounds.Height;

                g.Clear(DrawingColor.White);
                g.SmoothingMode = SmoothingMode.HighQuality;

                using DrawingFont titleFont = new("Arial", 16, DrawingFontStyle.Bold);
                using DrawingFont labelFont = new("Arial", 12, DrawingFontStyle.Bold);
                using DrawingFont valueFont = new("Arial", 16, DrawingFontStyle.Bold);
                using DrawingFont footerFont = new("Arial", 10, DrawingFontStyle.Italic);

                Pen pen = DrawingPens.Black;

                int marginHorizontal = 60;
                int marginVertical = 60;
                int printableWidth = pageWidth - (marginHorizontal * 2);
                int printableHeight = pageHeight - (marginVertical * 2);

                // Title header
                g.DrawString("[TEST PRINT - M-FAN ASSEMBLY LABEL]", titleFont, DrawingBrushes.Black, marginHorizontal, marginVertical);

                int contentStartY = marginVertical + 40;
                int contentHeight = printableHeight - 70;
                int rowCount = 5;
                int rowHeight = contentHeight / rowCount;

                int labelWidth = (int)(printableWidth * 0.35);
                int qrWidth = (int)(printableWidth * 0.30);
                int valueWidth = printableWidth - labelWidth - qrWidth;

                StringFormat leftMiddle = new()
                {
                    Alignment = StringAlignment.Near,
                    LineAlignment = StringAlignment.Center
                };

                string[] labels =
                {
                    "Mitsubishi Code",
                    "TRSS Code",
                    "Serial Number (MF)",
                    "Date Format",
                    "Origin"
                };

                string[] values =
                {
                    mitsubishiCode,
                    trssCode,
                    serialNumberCode,
                    dateFormat,
                    "MADE IN INDONESIA"
                };

                // Draw Table Grid & Data
                for (int i = 0; i < rowCount; i++)
                {
                    int currentY = contentStartY + (i * rowHeight);

                    // Label Column Box
                    g.DrawRectangle(pen, marginHorizontal, currentY, labelWidth, rowHeight);
                    g.DrawString(labels[i], labelFont, DrawingBrushes.Black, new Rectangle(marginHorizontal + 10, currentY, labelWidth - 15, rowHeight), leftMiddle);

                    // Value Column Box
                    g.DrawRectangle(pen, marginHorizontal + labelWidth, currentY, valueWidth, rowHeight);
                    g.DrawString(values[i], valueFont, DrawingBrushes.Black, new Rectangle(marginHorizontal + labelWidth + 10, currentY, valueWidth - 15, rowHeight), leftMiddle);
                }

                // QR Code Column Box
                g.DrawRectangle(pen, marginHorizontal + labelWidth + valueWidth, contentStartY, qrWidth, rowHeight * rowCount);

                int qrSize = Math.Min(qrWidth - 30, (rowHeight * rowCount) - 30);
                int qrX = marginHorizontal + labelWidth + valueWidth + ((qrWidth - qrSize) / 2);
                int qrY = contentStartY + (((rowHeight * rowCount) - qrSize) / 2);

                g.DrawImage(qrImage, qrX, qrY, qrSize, qrSize);

                // Footer
                int footerY = contentStartY + (rowHeight * rowCount) + 10;
                g.DrawString($"Printed on {printerNameStockIn} (Target: Test Mode) | {DateTime.Now:yyyy-MM-dd HH:mm:ss}", footerFont, DrawingBrushes.Gray, marginHorizontal, footerY);
            };

            pd.Print();
        }
        catch (ConnectionException ex)
        {
            _logger.LogError(ex, "Failed to connect to printer '{printerNameStockIn}' during test print", printerNameStockIn);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while test printing M-Fan Assy label on printer '{printerNameStockIn}'", printerNameStockIn);
            throw;
        }
    }

    public async Task PrintMFanAssyAsync(string serialNumberCode, List<string>? issueNumbers = null, CancellationToken cancellationToken = default)
    {
        var printHistoryDto = new PrintHistoryCreateMFanAssyDto
        {
            Status = PrintStatus.Success,
            SerialNumberCode = serialNumberCode,
        };

        try
        {
            await PrintMFanAssyLabelWithSdkAsync(serialNumberCode, cancellationToken);
            await _printHistoryService.CreateHistoryPrintMFanAssyAsync(printHistoryDto, cancellationToken);

            await _mqttPublisher.PublishAsync("data/print/m-fan-assy", new
            {
                IsPrinted = true,
                SerialNumber = serialNumberCode,
                IssueNumbers = issueNumbers ?? new List<string>()
            }, cancellationToken);

            await _mqttPublisher.PublishAsync("data/process/validation", new
            {
                status = true,
                process = "print-mfan",
                error = (string?)null,
                data = new
                {
                    serial_number = serialNumberCode
                }
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            printHistoryDto.Status = PrintStatus.Failed;
            printHistoryDto.ErrorMessage = ex.Message;
            await _printHistoryService.CreateHistoryPrintMFanAssyAsync(printHistoryDto, cancellationToken);

            await _mqttPublisher.PublishAsync("data/print/m-fan-assy", new
            {
                IsPrinted = false,
                SerialNumber = serialNumberCode,
                IssueNumbers = issueNumbers ?? new List<string>()
            }, cancellationToken);

            await _mqttPublisher.PublishAsync("data/process/validation", new
            {
                status = false,
                process = "print-mfan",
                error = ex.Message
            }, cancellationToken);
        }
    }

    public async Task RePrintAsync(int id, CancellationToken cancellation = default)
    {
        var result = await _printHistoryRepository.GetByIdAsync(id);
        if (result is null)
        {
            throw new NotFoundException("Print history not found", nameof(id));
        }
        try
        {
            if (result.Module == PrintModule.StockIn)
                await RePrintStockInAsync(result.ReferenceNumber!);
            else if (result.Module == PrintModule.Clinching)
                await RePrintClinchingAsync(result.ReferenceNumber!, cancellation);
            else if (result.Module == PrintModule.MFanAssy)
                await RePrintMFanAssyAsync(result.ReferenceNumber!, cancellation);

            result.Status = PrintStatus.Success;
            result.ErrorMessage = null;
            result.RetryCount += 1;
            result.LastRetryAt = DateTime.UtcNow;
            _printHistoryRepository.Update(result);
            await _printHistoryRepository.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            result.ErrorMessage = ex.Message;
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

    private async Task RePrintMFanAssyAsync(string serialNumberCode, CancellationToken cancellationToken = default)
    {
        var serialNumber = await _serialNumberRepo.GetWithRelatedBySerialNumberAsync(serialNumberCode);
        if (serialNumber is null)
            throw new KeyNotFoundException("Serial Number not found.");
        await PrintMFanAssyLabelWithSdkAsync(serialNumber.SerialNumberCode, cancellationToken);
    }
}
