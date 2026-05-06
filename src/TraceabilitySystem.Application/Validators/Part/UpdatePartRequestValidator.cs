using FluentValidation;
using TraceabilitySystem.Application.DTOs.Part;

namespace TraceabilitySystem.Application.Validators.Part;

public class UpdatePartRequestValidator : AbstractValidator<UpdatePartRequestDto>
{
    public UpdatePartRequestValidator()
    {
        RuleFor(x => x.Number)
            .NotEmpty().WithMessage("Part number cannot be empty.")
            .MaximumLength(50).WithMessage("Part number must not exceed 50 characters.")
            .When(x => x.Number != null);

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Part name cannot be empty.")
            .MaximumLength(100).WithMessage("Part name must not exceed 100 characters.")
            .When(x => x.Name != null);
            
        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.")
            .When(x => x.Description != null);
    }
}
