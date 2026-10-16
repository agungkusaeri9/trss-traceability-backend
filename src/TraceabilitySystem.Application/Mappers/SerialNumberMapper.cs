using System;
using System.Collections.Generic;
using System.Linq;
using TraceabilitySystem.Application.DTOs.SerialNumber;
using TraceabilitySystem.Domain.Entities;

namespace TraceabilitySystem.Application.Mappers
{
    public static class SerialNumberMapper
    {
        public static SerialNumberDto ToDto(this SerialNumber entity)
        {
            var issues = new List<SerialNumberIssueDto>();

            issues.AddRange(entity.Issues?.Select(x => new SerialNumberIssueDto
            {
                Id = x.Issue!.Id,
                Number = x.Issue.Number,
                StockInId = x.Issue.StockInId,
                PartNumber = x.Issue.StockIn!.Part!.Number,
                PartName = x.Issue.StockIn.Part.Name,
                CreatedAt = DateTime.SpecifyKind(x.Issue.CreatedAt, DateTimeKind.Utc),
                UpdatedAt = x.Issue.UpdatedAt.HasValue ? DateTime.SpecifyKind(x.Issue.UpdatedAt.Value, DateTimeKind.Utc) : null
            }) ?? []);

            if (entity.ParentRelations != null)
            {
                foreach (var rel in entity.ParentRelations)
                {
                    issues.AddRange(rel.ChildSerialNumber.Issues?.Select(x => new SerialNumberIssueDto
                    {
                        Id = x.Issue!.Id,
                        Number = x.Issue.Number,
                        StockInId = x.Issue.StockInId,
                        PartNumber = x.Issue.StockIn!.Part!.Number,
                        PartName = x.Issue.StockIn.Part.Name,
                        CreatedAt = DateTime.SpecifyKind(x.Issue.CreatedAt, DateTimeKind.Utc),
                        UpdatedAt = x.Issue.UpdatedAt.HasValue ? DateTime.SpecifyKind(x.Issue.UpdatedAt.Value, DateTimeKind.Utc) : null
                    }) ?? []);
                }
            }

            if (entity.ChildRelations != null)
            {
                foreach (var rel in entity.ChildRelations)
                {
                    issues.AddRange(rel.ParentSerialNumber.Issues?.Select(x => new SerialNumberIssueDto
                    {
                        Id = x.Issue!.Id,
                        Number = x.Issue.Number,
                        StockInId = x.Issue.StockInId,
                        PartNumber = x.Issue.StockIn!.Part!.Number,
                        PartName = x.Issue.StockIn.Part.Name,
                        CreatedAt = DateTime.SpecifyKind(x.Issue.CreatedAt, DateTimeKind.Utc),
                        UpdatedAt = x.Issue.UpdatedAt.HasValue ? DateTime.SpecifyKind(x.Issue.UpdatedAt.Value, DateTimeKind.Utc) : null
                    }) ?? []);
                }
            }

            return new SerialNumberDto
            {
                Id = entity.Id,
                SerialNumberCode = entity.SerialNumberCode,
                Type = entity.Type,
                CreatedAt = DateTime.SpecifyKind(entity.CreatedAt, DateTimeKind.Utc),
                CreatedBy = entity.CreatedBy,
                UpdatedAt = entity.UpdatedAt.HasValue ? DateTime.SpecifyKind(entity.UpdatedAt.Value, DateTimeKind.Utc) : null,
                UpdatedBy = entity.UpdatedBy,
                Issues = issues
            };
        }
    }
}
