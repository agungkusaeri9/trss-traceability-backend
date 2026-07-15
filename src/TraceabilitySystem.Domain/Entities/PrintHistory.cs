using System;
using System.Collections.Generic;
using System.Text;
using TraceabilitySystem.Domain.Enums;

namespace TraceabilitySystem.Domain.Entities
{
    public class PrintHistory
    {
        public int Id { get; set; }

        public PrintModule Module { get; set; }

        public int ReferenceId { get; set; }

        public string? ReferenceNumber { get; set; }

        public string PrinterName { get; set; } = default!;

        public PrintStatus Status { get; set; }

        public string? ErrorMessage { get; set; } = default!;

        public string? StackTrace { get; set; }

        public int RetryCount { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? LastRetryAt { get; set; }
    }
}
