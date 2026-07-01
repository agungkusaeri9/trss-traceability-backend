using System;
using System.Collections.Generic;
using System.Text;

namespace TraceabilitySystem.Worker
{
    public class MqttMessageLog
    {
        public string Topic { get; set; } = default!;
        public string Payload { get; set; } = default!;
        public string? OperatorUsername { get; set; }
        public string? ProcessName { get; set; }
        public bool? IsOk { get; set; }
        public string Status { get; set; } = "RECEIVED";
        public string? ErrorMessage { get; set; }
    }
}
