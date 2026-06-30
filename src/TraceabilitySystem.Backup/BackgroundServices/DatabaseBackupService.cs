using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace TraceabilitySystem.Backup.BackgroundServices;

public class DatabaseBackupService : BackgroundService
{
    private readonly ILogger<DatabaseBackupService> _logger;
    private readonly BackupSettings _settings;

    public DatabaseBackupService(
        ILogger<DatabaseBackupService> logger,
        IOptions<BackupSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DatabaseBackupService started. Interval: every {IntervalHours} hour(s).", _settings.IntervalHours);

        // Jalankan backup pertama langsung saat startup
        await RunBackupAsync(stoppingToken);
        await CleanupOldBackupsAsync();

        var interval = TimeSpan.FromHours(_settings.IntervalHours);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(interval, stoppingToken);
                await RunBackupAsync(stoppingToken);
                await CleanupOldBackupsAsync();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in backup loop.");
            }
        }

        _logger.LogInformation("DatabaseBackupService stopped.");
    }

    private async Task RunBackupAsync(CancellationToken cancellationToken)
    {
        try
        {
            var outputFolder = Path.GetFullPath(_settings.OutputFolder);
            Directory.CreateDirectory(outputFolder);

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var fileName = $"backup_{_settings.Database}_{timestamp}.sql";
            var filePath = Path.Combine(outputFolder, fileName);

            _logger.LogInformation("[Backup] Starting database backup → {FilePath}", filePath);

            var args = BuildMysqlDumpArgs(filePath);

            var psi = new ProcessStartInfo
            {
                FileName = _settings.MySqlDumpPath,
                Arguments = args,
                RedirectStandardOutput = false,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            // Pass password via environment variable agar tidak muncul di command line
            if (!string.IsNullOrWhiteSpace(_settings.Password))
            {
                psi.Environment["MYSQL_PWD"] = _settings.Password;
            }

            using var process = new Process { StartInfo = psi };
            process.Start();

            var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode == 0)
            {
                var fileSize = new FileInfo(filePath).Length;
                _logger.LogInformation("[Backup] ✓ Backup succeeded. File: {FileName} ({SizeKB} KB)", fileName, fileSize / 1024);
            }
            else
            {
                _logger.LogError("[Backup] ✗ mysqldump exited with code {ExitCode}. stderr: {Stderr}", process.ExitCode, stderr);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Backup] ✗ Backup failed: {Message}", ex.Message);
        }
    }

    private Task CleanupOldBackupsAsync()
    {
        try
        {
            var outputFolder = Path.GetFullPath(_settings.OutputFolder);
            if (!Directory.Exists(outputFolder))
                return Task.CompletedTask;

            var cutoffDate = DateTime.Now.Date.AddDays(-_settings.RetentionDays);
            var files = Directory.GetFiles(outputFolder, "backup_*.sql");
            int deleted = 0;

            foreach (var file in files)
            {
                var fileName = Path.GetFileNameWithoutExtension(file);

                // Format: backup_{database}_{yyyy-MM-dd}_{HH-mm-ss}
                // Split by '_' langsung menghasilkan "2026-06-30" sebagai satu segment
                var parts = fileName.Split('_');

                DateOnly fileDate = default;
                bool parsed = false;
                foreach (var part in parts)
                {
                    if (DateOnly.TryParseExact(
                            part,
                            "yyyy-MM-dd",
                            System.Globalization.CultureInfo.InvariantCulture,
                            System.Globalization.DateTimeStyles.None,
                            out fileDate))
                    {
                        parsed = true;
                        break;
                    }
                }

                if (!parsed)
                {
                    _logger.LogWarning("[Cleanup] Cannot parse date from filename: {File}, skipping.", Path.GetFileName(file));
                    continue;
                }

                if (fileDate.ToDateTime(TimeOnly.MinValue) < cutoffDate)
                {
                    File.Delete(file);
                    deleted++;
                    _logger.LogInformation("[Cleanup] Deleted old backup: {File} (date: {Date})", Path.GetFileName(file), fileDate);
                }
            }

            if (deleted > 0)
                _logger.LogInformation("[Cleanup] Removed {Count} old backup file(s) older than {Days} day(s).", deleted, _settings.RetentionDays);
            else
                _logger.LogInformation("[Cleanup] No old backups to remove (retention: {Days} day(s)).", _settings.RetentionDays);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Cleanup] ✗ Cleanup failed: {Message}", ex.Message);
        }

        return Task.CompletedTask;
    }

    private string BuildMysqlDumpArgs(string outputFilePath)
    {
        // Gunakan redirect output ke file agar tidak ada masalah encoding
        var args = $"--host={_settings.Host} --port={_settings.Port} --user={_settings.Username}";

        if (!string.IsNullOrWhiteSpace(_settings.Password))
        {
            // Lewat environment variable MYSQL_PWD, jadi tidak perlu --password di sini
        }
        else
        {
            args += " --password=";
        }

        args += $" --single-transaction --routines --triggers --result-file=\"{outputFilePath}\" {_settings.Database}";

        return args;
    }
}
