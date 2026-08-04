using FluentValidation;
using GardenSystem.Application.Plants.Commands;

namespace GardenSystem.Application.Plants.Validators;

public sealed class CreatePlantCommandValidator : AbstractValidator<CreatePlantCommand>
{
    public CreatePlantCommandValidator()
    {
        RuleFor(x => x.GardenId)
            .NotEmpty();

        RuleFor(x => x.PlantName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Species)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.PlantType)
            .IsInEnum();

        RuleFor(x => x.SurfaceAreaRequired)
            .GreaterThan(0);

        RuleFor(x => x.IdealHumidityLevel)
            .InclusiveBetween(0, 100);
    }
}
