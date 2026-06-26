using Mapster;
using TraceabilitySystem.Application.DTOs.ProcessLog;
using TraceabilitySystem.Application.Interfaces;
using TraceabilitySystem.Domain.Entities;
using TraceabilitySystem.Domain.Interfaces;
using TraceabilitySystem.Shared.Exceptions;
using TraceabilitySystem.Shared.Models;

namespace TraceabilitySystem.Application.Services;

public class ProcessLogService : IProcessLogService
{
    private readonly IProcessLogRepository _processLogRepository;
    private readonly IIssueRepository _issueRepository;
    private readonly IStockInRepository _stockInRepository;
    private readonly ISerialNumberRepository _serialNumberRepository;

    public ProcessLogService(
        IProcessLogRepository processLogRepository,
        IIssueRepository issueRepository,
        IStockInRepository stockInRepository,
        ISerialNumberRepository serialNumberRepository)
    {
        _processLogRepository = processLogRepository;
        _issueRepository = issueRepository;
        _stockInRepository = stockInRepository;
        _serialNumberRepository = serialNumberRepository;
    }

    public async Task<PagedResult<ProcessLogDto>> GetProcessLogsAsync(
        int page,
        int pageSize,
        string? serialNumberCode = null,
        string? partNumber = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var (logs, totalCount) = await _processLogRepository.GetPagedLogsAsync(
            page, pageSize, serialNumberCode, partNumber, isActive, cancellationToken);

        var dtos = new List<ProcessLogDto>();

        foreach (var log in logs)
        {
            var dto = log.Adapt<ProcessLogDto>();
            
            // Fetch extra info (Part Number/Name) based on SerialNumber
            var serialNumber = await _serialNumberRepository.GetWithRelatedAsync(log.SerialNumberId, cancellationToken);
            
            if (serialNumber != null && serialNumber.Issues.Any())
            {
                // Take the first issue's stock in part
                var firstIssue = serialNumber.Issues.FirstOrDefault();
                if (firstIssue?.Issue?.StockIn?.Part != null)
                {
                    dto.PartNumber = firstIssue.Issue.StockIn.Part.Number;
                    dto.PartName = firstIssue.Issue.StockIn.Part.Name;
                }
            }

            dtos.Add(dto);
        }

        return new PagedResult<ProcessLogDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ProcessLogDto> GetProcessLogByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var log = await _processLogRepository.GetLogWithDetailsAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(ProcessLog), id);

        var dto = log.Adapt<ProcessLogDto>();

        // Map details with display values
        var flatDetails = log.Details.Select(d => 
        {
            var detailDto = d.Adapt<ProcessLogDetailDto>();
            detailDto.ProcessName = d.Process?.Name;
            detailDto.ParameterName = d.Parameter?.Name;
            detailDto.DataType = d.Parameter?.DataType;

            // Format display value
            if (detailDto.DataType == "boolean")
            {
                detailDto.DisplayValue = (d.ValueBoolean == true) ? "OK" : "NG";
            }
            else if (detailDto.DataType == "number")
            {
                detailDto.DisplayValue = d.ValueNumber?.ToString("N2");
            }
            else
            {
                detailDto.DisplayValue = d.ValueText;
            }

            return detailDto;
        }).ToList();

        // Group by ProcessName and then by Parameter
        dto.Details = flatDetails
            .GroupBy(d => d.ProcessName ?? "Unknown Process")
            .Select(g => new ProcessGroupDto
            {
                ProcessName = g.Key,
                Parameters = g.GroupBy(p => new { p.ParameterId, p.ParameterName, p.DataType })
                    .Select(pg => new ProcessParameterValueDto
                    {
                        ParameterId = pg.Key.ParameterId,
                        ParameterName = pg.Key.ParameterName,
                        DataType = pg.Key.DataType,
                        Values = pg.Select(v => v.DataType switch
                        {
                            "boolean" => (object?)v.ValueBoolean,
                            "number" => (object?)v.ValueNumber,
                            _ => (object?)v.ValueText
                        }).ToList()
                    }).ToList()
            }).ToList();
        
        // Fetch Part info from SerialNumber
        var serialNumber = await _serialNumberRepository.GetWithRelatedAsync(log.SerialNumberId, cancellationToken);
        
        if (serialNumber != null && serialNumber.Issues.Any())
        {
            var firstIssue = serialNumber.Issues.FirstOrDefault();
            if (firstIssue?.Issue?.StockIn?.Part != null)
            {
                dto.PartNumber = firstIssue.Issue.StockIn.Part.Number;
                dto.PartName = firstIssue.Issue.StockIn.Part.Name;
            }
        }

        return dto;
    }
}
