using FluentValidation;
using TraceabilitySystem.Application.DTOs.StockInRework;

namespace TraceabilitySystem.Worker.Validator.StockInRework
{
    public class CreateStockInReworkValidator : AbstractValidator<CreateStockInReworkDto>
    {
        public CreateStockInReworkValidator()
        {
            RuleFor(x => x.SerialNumberCode)
                .NotEmpty()
                .WithMessage("SerialNumberCode is required.");

            RuleFor(x => x.IssueNumbers)
                .NotEmpty()
                .WithMessage("IssueNumbers is required.")
                .Must(issues => issues.Any(i => !i.Status))
                .WithMessage("At least one issue number must have Status = false (NG).");
        }
    }
}
