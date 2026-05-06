using FluentValidation;
using TraceabilitySystem.Application.DTOs.Part;

namespace TraceabilitySystem.Application.Validators.Part;

public class CreatePartRequestValidator : AbstractValidator<CreatePartRequestDto>
{
    public CreatePartRequestValidator()
    {
        RuleFor(x => x.Number)
            .NotEmpty().WithMessage("Part number is required.")
            .MaximumLength(50).WithMessage("Part number must not exceed 50 characters.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Part name is required.")
            .MaximumLength(100).WithMessage("Part name must not exceed 100 characters.");
            
        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.");
    }
}
