using System;
using System.Collections.Generic;
using System.Text;
using TraceabilitySystem.Application.DTOs.SerialNumber;
using TraceabilitySystem.Domain.Entities;

namespace TraceabilitySystem.Application.Mappers
{
    public static class SerialNumberMapper
    {
        public static SerialNumberDto ToDto(this SerialNumber entity)
        {
            return new SerialNumberDto
            {
                Id = entity.Id,
                SerialNumberCode = entity.SerialNumberCode,
                Type = entity.Type,
                CreatedAt = entity.CreatedAt,
                CreatedBy = entity.CreatedBy,
                UpdatedAt = entity.UpdatedAt,
                UpdatedBy = entity.UpdatedBy,

                Issues = entity.Issues.Select(x => new SerialNumberIssueDto
                {
                    Id = x.Issue!.Id,
                    Number = x.Issue.Number,
                    StockInId = x.Issue.StockInId,
                    PartNumber = x.Issue.StockIn!.Part!.Number,
                    PartName = x.Issue.StockIn.Part.Name,
                    CreatedAt = x.Issue.CreatedAt,
                    UpdatedAt = x.Issue.UpdatedAt
                }).ToList()
            };
        }
    }
}
