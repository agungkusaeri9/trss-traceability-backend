using System;
using System.Collections.Generic;
using System.Text;
using TraceabilitySystem.Application.DTOs.SerialNumber;
using TraceabilitySystem.Domain.Enums;

namespace TraceabilitySystem.Application.DTOs.PrintHistory
{
    public class PrintHistoryCreateDto
    {
        public PrintModule Module { get; set; }

        public int ReferenceId { get; set; }

        public string? ReferenceNumber { get; set; }

        public string PrinterName { get; set; } = default!;

        public PrintStatus Status { get; set; }

        public string? ErrorMessage { get; set; }

        public string? StackTrace { get; set; }

        public int RetryCount { get; set; } = 0;
        public DateTime? CreatedAt { get; set; }
    }

    public class PrintHistoryCreateClinchingDto
    {
        public PrintStatus Status { get; set; } = PrintStatus.Failed;
        public string PrintName { get; set; } = string.Empty;
        public string SerialNumberCode { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; } 
    }

    public class PrintHistoryCreateStockInDto
    {
        public PrintStatus Status { get; set; } = PrintStatus.Failed;
        public string PrintName { get; set; } = string.Empty;
        public string IssueNumber { get; set; } = string.Empty;
        public string? ErrorMessage { get; set; }
    }
}
