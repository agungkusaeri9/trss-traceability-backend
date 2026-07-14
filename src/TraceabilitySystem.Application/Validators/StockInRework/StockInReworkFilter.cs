using System;
using System.Collections.Generic;
using System.Text;
using FluentValidation;
using TraceabilitySystem.Application.DTOs.StockInRework;

namespace TraceabilitySystem.Application.Validators.StockInRework
{
    public class StockInReworkFilterValidator : AbstractValidator<FilterStockInReworkDto>
    {
        public StockInReworkFilterValidator()
        {
            RuleFor(x => x.Disposition)
                .IsInEnum()
                .When(x => x.Disposition.HasValue);
        }
    }
}
