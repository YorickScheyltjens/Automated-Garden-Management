using FluentValidation;
using GardenSystem.Application.Auth.Commands;

namespace GardenSystem.Application.Auth.Validators;

public sealed class RefreshCommandValidator : AbstractValidator<RefreshCommand>
{
    public RefreshCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty();
    }
}
