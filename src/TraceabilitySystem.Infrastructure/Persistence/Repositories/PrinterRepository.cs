using TraceabilitySystem.Domain.Entities;
using TraceabilitySystem.Domain.Interfaces;

namespace TraceabilitySystem.Infrastructure.Persistence.Repositories;

public class PrinterRepository : BaseRepository<Printer>, IPrinterRepository
{
    public PrinterRepository(AppDbContext context) : base(context) { }

}
