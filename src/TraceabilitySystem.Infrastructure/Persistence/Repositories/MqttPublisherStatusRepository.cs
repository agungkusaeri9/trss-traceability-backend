using TraceabilitySystem.Domain.Entities;
using TraceabilitySystem.Domain.Interfaces;

namespace TraceabilitySystem.Infrastructure.Persistence.Repositories;

public class MqttPublisherStatusRepository : BaseRepository<MqttPublisherStatus>, IMqttPublisherStatusRepository
{
    public MqttPublisherStatusRepository(AppDbContext context) : base(context)
    {
    }
}
