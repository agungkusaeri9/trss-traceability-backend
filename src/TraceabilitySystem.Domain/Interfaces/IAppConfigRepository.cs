using TraceabilitySystem.Domain.Entities;

namespace TraceabilitySystem.Domain.Interfaces;

public interface IAppConfigRepository : IRepository<AppConfig>
{
    Task<AppConfig?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
    Task<string> GetPrinterNameClinching(CancellationToken cancellationToken = default);
}
