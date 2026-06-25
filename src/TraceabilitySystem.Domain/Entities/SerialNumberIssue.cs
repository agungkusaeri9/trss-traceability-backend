using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilitySystem.Domain.Entities
{
    public class SerialNumberIssue
    {
        public int Id { get; set; }
        public int SerialNumberId { get; set; }
        public int IssueId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; }

        // Navigation
        public virtual SerialNumber SerialNumber { get; set; } = null!; 
        public virtual Issue Issue { get; set; } = null!;
    }

}