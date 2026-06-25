using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilitySystem.Domain.Entities
{
    public class SerialNumberRelation
    {
        public int Id { get; set; }
        public int ParentSerialNumberId { get; set; }
        public int ChildSerialNumberId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CreatedBy { get; set; } 
        // Navigation
        public virtual SerialNumber ParentSerialNumber { get; set; } = null!;
        public virtual SerialNumber ChildSerialNumber { get; set; } = null!;
    }
}