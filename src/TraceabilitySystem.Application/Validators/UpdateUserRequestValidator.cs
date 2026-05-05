using FluentValidation;
using TraceabilitySystem.Application.DTOs;

namespace TraceabilitySystem.Application.Validators;

public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.Name)
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.")
            .When(x => x.Name != null);

        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username cannot be empty.")
            .When(x => x.Username != null);
    }
}
