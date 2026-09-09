using Microsoft.EntityFrameworkCore;
using TraceabilitySystem.Domain.Entities;
using TraceabilitySystem.Domain.Interfaces;

namespace TraceabilitySystem.Infrastructure.Persistence.Repositories;

public class AppConfigRepository : BaseRepository<AppConfig>, IAppConfigRepository
{
    public AppConfigRepository(AppDbContext context) : base(context) { }

    public async Task<AppConfig?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(x => x.Key == key, cancellationToken);
    }

    public async Task<string> GetPrinterNameClinching(CancellationToken cancellationToken = default)
    {
        var config = await _dbSet.FirstOrDefaultAsync(x => x.Key == "PRINTER_NAME_CLINCHING");
        return config?.Value ?? string.Empty;
    }

    public async Task<string> GetPrinterNameStockIn(CancellationToken cancellationToken = default)
    {
        var config = await _dbSet.FirstOrDefaultAsync(x => x.Key == "PRINTER_NAME_STOCK_IN");
        return config?.Value ?? string.Empty;
    }

    public async Task<string> GetPrinterNameMFanAssy(CancellationToken cancellationToken = default)
    {
        var config = await _dbSet.FirstOrDefaultAsync(x => x.Key == "PRINTER_NAME_M_FAN_ASSY" || x.Key == "PRINTER_NAME_MFAN_ASSY", cancellationToken);
        return config?.Value ?? string.Empty;
    }

    public async Task<string> GetPrinterClinchingIpAsync(CancellationToken cancellationToken = default)
    {
        var config = await _dbSet.FirstOrDefaultAsync(x => x.Key == "PRINTER_IP_CLINCHING", cancellationToken);
        return config?.Value?.Trim() ?? string.Empty;
    }

    public async Task<int> GetPrinterClinchingPortAsync(CancellationToken cancellationToken = default)
    {
        var config = await _dbSet.FirstOrDefaultAsync(x => x.Key == "PRINTER_PORT_CLINCHING", cancellationToken);
        if (config != null && int.TryParse(config.Value, out int port) && port > 0)
        {
            return port;
        }
        return 9100;
    }

    public async Task<string> GetPrinterMFanAssyIpAsync(CancellationToken cancellationToken = default)
    {
        var config = await _dbSet.FirstOrDefaultAsync(x => x.Key == "PRINTER_IP_M_FAN_ASSY" || x.Key == "PRINTER_IP_MFAN_ASSY", cancellationToken);
        return config?.Value?.Trim() ?? string.Empty;
    }

    public async Task<int> GetPrinterMFanAssyPortAsync(CancellationToken cancellationToken = default)
    {
        var config = await _dbSet.FirstOrDefaultAsync(x => x.Key == "PRINTER_PORT_M_FAN_ASSY" || x.Key == "PRINTER_PORT_MFAN_ASSY", cancellationToken);
        if (config != null && int.TryParse(config.Value, out int port) && port > 0)
        {
            return port;
        }
        return 9100;
    }

    public async Task<bool> GetIsTestModeMFanAssyAsync(CancellationToken cancellationToken = default)
    {
        var config = await _dbSet.FirstOrDefaultAsync(x => x.Key == "PRINTER_TEST_MODE_MFAN_ASSY" || x.Key == "IS_TEST_PRINT_MFAN_ASSY", cancellationToken);
        if (config == null || string.IsNullOrWhiteSpace(config.Value)) return false;
        var val = config.Value.Trim().ToLowerInvariant();
        return val == "true" || val == "1" || val == "yes" || val == "on";
    }
}
