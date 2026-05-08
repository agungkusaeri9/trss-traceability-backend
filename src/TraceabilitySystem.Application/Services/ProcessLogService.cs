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

    public ProcessLogService(
        IProcessLogRepository processLogRepository,
        IIssueRepository issueRepository,
        IStockInRepository stockInRepository)
    {
        _processLogRepository = processLogRepository;
        _issueRepository = issueRepository;
        _stockInRepository = stockInRepository;
    }

    public async Task<PagedResult<ProcessLogDto>> GetProcessLogsAsync(
        int page,
        int pageSize,
        string? issueNo = null,
        string? partNumber = null,
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var (logs, totalCount) = await _processLogRepository.GetPagedLogsAsync(
            page, pageSize, issueNo, partNumber, isActive, cancellationToken);

        var dtos = new List<ProcessLogDto>();

        foreach (var log in logs)
        {
            var dto = log.Adapt<ProcessLogDto>();
            
            // Fetch extra info (Part Number/Name) based on IssueNo
            var issue = await _issueRepository.FirstOrDefaultAsync(i => i.Number == log.IssueNo, cancellationToken);
            if (issue != null)
            {
                var stockIn = await _stockInRepository.GetByIdAsync(issue.StockInId, cancellationToken);
                if (stockIn?.Part != null)
                {
                    dto.PartNumber = stockIn.Part.Number;
                    dto.PartName = stockIn.Part.Name;
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
            ?? throw new NotFoundException(nameof(ProcessLog), (int)id);

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
        var issue = await _issueRepository.FirstOrDefaultAsync(i => i.Number == log.IssueNo, cancellationToken);
        if (issue != null)
        {
            var stockIn = await _stockInRepository.GetByIdAsync(issue.StockInId, cancellationToken);
            if (stockIn?.Part != null)
            {
                dto.PartNumber = stockIn.Part.Number;
                dto.PartName = stockIn.Part.Name;
            }
        }

        return dto;
    }
}
