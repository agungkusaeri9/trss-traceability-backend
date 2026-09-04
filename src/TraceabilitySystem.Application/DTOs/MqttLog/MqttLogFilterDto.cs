using System;
using TraceabilitySystem.Application.DTOs.Pagination;

namespace TraceabilitySystem.Application.DTOs.MqttLog;

public enum MqttLogStatus
{
    RECEIVED,
    PROCESSING,
    SUCCESS,
    FAILED
}

public class MqttLogFilterDto : PaginationDto
{
    public string? Topic { get; set; }
    public string? ProcessName { get; set; }
    public MqttLogStatus? Status { get; set; }
    public string? OperatorUsername { get; set; }
    public bool? IsOk { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Search { get; set; }
}
