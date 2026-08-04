using FluentValidation;
using GardenSystem.Application.Gardens.Commands;

namespace GardenSystem.Application.Gardens.Validators;

public sealed class UpdateGardenCommandValidator : AbstractValidator<UpdateGardenCommand>
{
    public UpdateGardenCommandValidator()
    {
        RuleFor(x => x.GardenId)
            .NotEmpty();

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
