using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using TraceabilitySystem.Domain.Entities;

namespace TraceabilitySystem.Domain.Interfaces;

public interface ISerialNumberRepository : IRepository<SerialNumber>
{
    Task<SerialNumber?> GetWithRelatedAsync(int id, CancellationToken cancellationToken = default);
    Task<SerialNumber?> GetWithRelatedBySerialNumberAsync(string serialNumber, CancellationToken cancellationToken = default);
    Task<IEnumerable<SerialNumber>> GetAllWithChildRelationsAsync(CancellationToken cancellationToken = default);
    Task CreateWithIssuesAsync(
        IEnumerable<SerialNumber> serialNumbers,
        IEnumerable<string> issueNumbers,
        CancellationToken cancellationToken = default);

    Task<bool> CheckByCodeAsync(string serialNumberCode, CancellationToken cancellationToken = default);
    Task<(IEnumerable<SerialNumber> Items, int TotalCount)> GetPagedWithRelatedAsync(
     int page,
     int pageSize,
     Expression<Func<SerialNumber, bool>>? predicate = null,
     Func<IQueryable<SerialNumber>, IOrderedQueryable<SerialNumber>>? orderBy = null,
     CancellationToken cancellationToken = default);
}
