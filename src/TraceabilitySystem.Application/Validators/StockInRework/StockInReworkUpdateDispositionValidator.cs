using System;
using System.Collections.Generic;
using System.Text;
using FluentValidation;
using TraceabilitySystem.Application.DTOs.StockInRework;

namespace TraceabilitySystem.Worker.Validator.StockInRework
{
    public class StockInReworkValidator : AbstractValidator<UpdateStockInReworkDto>
    {
        public StockInReworkValidator()
        {
            RuleFor(x => x.Disposition)
               .NotEmpty()
               .Must(x => x == "STOCK_IN" || x == "SCRAP")
               .WithMessage("Disposition must be either 'STOCK_IN' or 'SCRAP'.");
        }
    }
}
