using System;
using System.Collections.Generic;
using System.Text;
using TraceabilitySystem.Domain.Entities;
using TraceabilitySystem.Domain.Interfaces;

namespace TraceabilitySystem.Infrastructure.Persistence.Repositories
{
    public class PrintHistoryRepository : BaseRepository<PrintHistory>, IPrintHistoryRepository
    {
        public PrintHistoryRepository(AppDbContext context) : base(context) { }
    }


}

