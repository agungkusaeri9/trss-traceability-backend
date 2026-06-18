using System.Threading;
using System.Threading.Tasks;

namespace TraceabilitySystem.Application.Interfaces;

public interface ITraceabilitySummaryNotifier
{
    Task BroadcastAsync(CancellationToken cancellationToken = default);
}
