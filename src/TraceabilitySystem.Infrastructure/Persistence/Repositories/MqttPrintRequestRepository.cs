using TraceabilitySystem.Domain.Entities;
using TraceabilitySystem.Domain.Interfaces;

namespace TraceabilitySystem.Infrastructure.Persistence.Repositories;

public class MqttPrintRequestRepository : BaseRepository<MqttPrintRequest>, IMqttPrintRequestRepository
{
    public MqttPrintRequestRepository(AppDbContext context) : base(context)
    {
    }
}
