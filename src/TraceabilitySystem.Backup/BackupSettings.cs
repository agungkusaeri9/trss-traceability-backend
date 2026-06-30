namespace TraceabilitySystem.Backup;

public class BackupSettings
{
    /// <summary>Folder output untuk menyimpan file backup SQL.</summary>
    public string OutputFolder { get; set; } = "backups";

    /// <summary>Interval backup dalam jam (default 4 jam).</summary>
    public int IntervalHours { get; set; } = 4;

    /// <summary>Jumlah hari backup yang disimpan. File lebih lama dari ini akan dihapus (default 7 hari).</summary>
    public int RetentionDays { get; set; } = 7;

    /// <summary>Path ke executable mysqldump. Sesuaikan jika tidak ada di PATH.</summary>
    public string MySqlDumpPath { get; set; } = "mysqldump";

    public string Host { get; set; } = "localhost";
    public string Port { get; set; } = "3306";
    public string Database { get; set; } = "trss_traceability_system";
    public string Username { get; set; } = "root";
    public string Password { get; set; } = "";
}
