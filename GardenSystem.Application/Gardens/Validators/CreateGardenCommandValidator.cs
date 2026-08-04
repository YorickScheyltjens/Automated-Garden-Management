using FluentValidation;
using GardenSystem.Application.Gardens.Commands;

namespace GardenSystem.Application.Gardens.Validators;

public sealed class CreateGardenCommandValidator : AbstractValidator<CreateGardenCommand>
{
    public CreateGardenCommandValidator()
    {
        RuleFor(x => x.GardenName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.TotalSurfaceArea)
            .GreaterThan(0);

        RuleFor(x => x.TargetHumidityLevel)
            .InclusiveBetween(0, 100);

        RuleFor(x => x.LocationDescription)
            .NotEmpty()
            .MaximumLength(500);
    }
}
