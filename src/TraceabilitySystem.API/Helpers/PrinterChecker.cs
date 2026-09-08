using System.Drawing.Printing;
using System.Management;
using System.Net.Sockets;
using System.Runtime.Versioning;
using TraceabilitySystem.Application.DTOs.Printer;
using TraceabilitySystem.Domain.Interfaces;
using Zebra.Sdk.Comm;
using Zebra.Sdk.Printer;

namespace TraceabilitySystem.API.Helpers;

public static class PrinterChecker
{
    public static async Task<List<PrinterStatusDto>> CheckAppConfigPrintersAsync(
        IAppConfigRepository configRepo,
        CancellationToken cancellationToken = default)
    {
        var result = new List<PrinterStatusDto>();

        // 1. Stock In Printer (Koneksi By Name / Windows Driver)
        var stockInName = await configRepo.GetPrinterNameStockIn(cancellationToken);
        var (stockInOnline, stockInError) = CheckWindowsPrinter(stockInName);

        result.Add(new PrinterStatusDto
        {
            Key = "STOCK_IN",
            Name = "Printer Stock In",
            PrinterName = stockInName,
            IsOnline = stockInOnline,
            Status = stockInOnline ? "Online" : "Offline",
            ErrorMessage = stockInError,
            LastChecked = DateTime.UtcNow
        });

        // 2. Clinching Printer (Koneksi By IP & Zebra Protocol)
        var clinchingIp = await configRepo.GetPrinterClinchingIpAsync(cancellationToken);
        var clinchingPort = await configRepo.GetPrinterClinchingPortAsync(cancellationToken);
        var clinchingName = await configRepo.GetPrinterNameClinching(cancellationToken);

        var (clinchingOnline, clinchingError) = await CheckZebraIpPrinterAsync(clinchingIp, clinchingPort, 2000);

        result.Add(new PrinterStatusDto
        {
            Key = "CLINCHING",
            Name = "Printer Clinching",
            PrinterName = clinchingName,
            IpAddress = clinchingIp,
            Port = clinchingPort,
            IsOnline = clinchingOnline,
            Status = clinchingOnline ? "Online" : "Offline",
            ErrorMessage = clinchingError,
            LastChecked = DateTime.UtcNow
        });

        // 3. M-Fan Assy Printer (Koneksi By IP & Zebra Protocol)
        var mfanIp = await configRepo.GetPrinterMFanAssyIpAsync(cancellationToken);
        var mfanPort = await configRepo.GetPrinterMFanAssyPortAsync(cancellationToken);
        var mfanName = await configRepo.GetPrinterNameMFanAssy(cancellationToken);

        var (mfanOnline, mfanError) = await CheckZebraIpPrinterAsync(mfanIp, mfanPort, 2000);

        result.Add(new PrinterStatusDto
        {
            Key = "MFAN_ASSY",
            Name = "Printer M-Fan Assy",
            PrinterName = mfanName,
            IpAddress = mfanIp,
            Port = mfanPort,
            IsOnline = mfanOnline,
            Status = mfanOnline ? "Online" : "Offline",
            ErrorMessage = mfanError,
            LastChecked = DateTime.UtcNow
        });

        return result;
    }

    [SupportedOSPlatform("windows")]
    public static (bool isOnline, string? errorMessage) CheckWindowsPrinter(string? printerName)
    {
        if (string.IsNullOrWhiteSpace(printerName))
        {
            return (false, "PRINTER_NAME_STOCK_IN is not configured");
        }

        try
        {
            // Cek apakah driver / printer terdaftar di Windows
            var settings = new PrinterSettings { PrinterName = printerName };
            if (!settings.IsValid)
            {
                return (false, $"Printer '{printerName}' is not found in Windows");
            }

            // Cek status WMI Win32_Printer (apakah WorkOffline bernilai true)
            using var searcher = new ManagementObjectSearcher(
                $"SELECT Name, WorkOffline, PrinterStatus, ExtendedPrinterStatus, DetectedErrorState FROM Win32_Printer WHERE Name = '{printerName.Replace("\\", "\\\\")}'");

            var printer = searcher.Get().Cast<ManagementObject>().FirstOrDefault();
            if (printer == null)
            {
                return (false, $"Printer '{printerName}' not found via WMI");
            }

            if (printer["WorkOffline"] is bool isOffline && isOffline)
            {
                return (false, $"Printer '{printerName}' is offline");
            }

            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, $"Error checking printer '{printerName}': {ex.Message}");
        }
    }

    public static Task<(bool isOnline, string? errorMessage)> CheckZebraIpPrinterAsync(string ipAddress, int port, int timeoutMs = 2000)
    {
        if (string.IsNullOrWhiteSpace(ipAddress) || port <= 0)
        {
            return Task.FromResult((false, (string?)"Printer IP/Port is not configured"));
        }

        return Task.Run(() =>
        {
            Connection? connection = null;
            try
            {
                connection = new TcpConnection(ipAddress, port, timeoutMs, 1000);
                connection.Open();

                var printer = ZebraPrinterFactory.GetInstance(connection);
                var status = printer.GetCurrentStatus();

                if (!status.isReadyToPrint)
                {
                    if (status.isHeadOpen) return (false, "Printer error: Head is open");
                    if (status.isPaperOut) return (false, "Printer error: Paper/Ribbon is out");
                    if (status.isPaused) return (false, "Printer error: Printer is paused");
                    return (false, "Printer is not ready to print");
                }

                return (true, (string?)null);
            }
            catch (Exception ex)
            {
                return (false, (string?)$"Cannot connect to {ipAddress}:{port} ({ex.Message})");
            }
            finally
            {
                try
                {
                    connection?.Close();
                }
                catch
                {
                    // Ignore close errors
                }
            }
        });
    }
}

