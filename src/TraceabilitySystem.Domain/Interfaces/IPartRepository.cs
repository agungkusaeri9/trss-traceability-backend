using TraceabilitySystem.Domain.Entities;

namespace TraceabilitySystem.Domain.Interfaces;

public interface IPartRepository : IRepository<Part>
{

    Task RemoveAsync(Part part, CancellationToken token);
    Task<Part> GetDetailByIdAsync(int id, CancellationToken cancellationToken = default);


}