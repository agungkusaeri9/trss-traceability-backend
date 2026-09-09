using TraceabilitySystem.Domain.Entities;

namespace TraceabilitySystem.Domain.Interfaces;

public interface IAppConfigRepository : IRepository<AppConfig>
{
    Task<AppConfig?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
    Task<string> GetPrinterNameClinching(CancellationToken cancellationToken = default);
    Task<string> GetPrinterNameStockIn(CancellationToken cancellationToken = default);
    Task<string> GetPrinterNameMFanAssy(CancellationToken cancellationToken = default);
    Task<string> GetPrinterClinchingIpAsync(CancellationToken cancellationToken = default);
    Task<int> GetPrinterClinchingPortAsync(CancellationToken cancellationToken = default);
    Task<string> GetPrinterMFanAssyIpAsync(CancellationToken cancellationToken = default);
    Task<int> GetPrinterMFanAssyPortAsync(CancellationToken cancellationToken = default);
    Task<bool> GetIsTestModeMFanAssyAsync(CancellationToken cancellationToken = default);
}
