using System;
using TraceabilitySystem.Domain.Enums;

namespace TraceabilitySystem.Application.DTOs.PrintHistory
{
    public class PrintHistoryDto
    {
        public int Id { get; set; }

        public PrintModule Module { get; set; }

        public int ReferenceId { get; set; }

        public string? ReferenceNumber { get; set; }

        public string PrinterName { get; set; } = default!;

        public PrintStatus Status { get; set; }

        public string? ErrorMessage { get; set; }

        public string? StackTrace { get; set; }

        public int RetryCount { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? LastRetryAt { get; set; }
    }


}