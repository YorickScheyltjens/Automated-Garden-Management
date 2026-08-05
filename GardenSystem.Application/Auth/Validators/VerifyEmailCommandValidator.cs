using FluentValidation;
using GardenSystem.Application.Auth.Commands;

namespace GardenSystem.Application.Auth.Validators;

public sealed class VerifyEmailCommandValidator : AbstractValidator<VerifyEmailCommand>
{
    public VerifyEmailCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(320);

        RuleFor(x => x.Code)
            .NotEmpty()
            .Matches(@"^\d{6}$")
            .WithMessage("'code' must be exactly 6 digits.");
    }
}
