using FluentValidation;
using GardenSystem.Application.Reports.Queries;

namespace GardenSystem.Application.Reports.Validators;

public sealed class GetPlantChangesQueryValidator : AbstractValidator<GetPlantChangesQuery>
{
    public GetPlantChangesQueryValidator()
    {
        RuleFor(x => x.Since)
            .NotNull()
            .WithMessage("'since' is required.");
    }
}
