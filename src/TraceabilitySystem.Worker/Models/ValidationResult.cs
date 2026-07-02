using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilitySystem.Worker.Models
{
    public class ValidationResult
    {
        public bool IsValid => !Errors.Any();

        public string ProcessCode { get; set; } = string.Empty;

        public string? SerialNumber { get; set; }

        public List<string> Errors { get; set; } = new();
    }
}
