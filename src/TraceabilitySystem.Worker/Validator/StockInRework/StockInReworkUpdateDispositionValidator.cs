using System;
using System.Collections.Generic;
using System.Text;
using FluentValidation;
using TraceabilitySystem.Application.DTOs.StockInRework;

namespace TraceabilitySystem.Worker.Validator.StockInRework
{
    public class StockInReworkUpdateDispositionValidator : AbstractValidator<UpdateStockInReworkDto>
    {
        public StockInReworkUpdateDispositionValidator()
        {
            RuleFor(x => x.Disposition)
               .NotEmpty()
               .Must(x => x == "STOCK_IN" || x == "SCRAP")
               .WithMessage("Disposition must be either 'StockIn' or 'Scrap'.");
        }
    }
}
