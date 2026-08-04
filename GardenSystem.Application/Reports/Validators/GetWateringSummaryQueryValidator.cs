using FluentValidation;
using GardenSystem.Application.Reports.Queries;

namespace GardenSystem.Application.Reports.Validators;

public sealed class GetWateringSummaryQueryValidator : AbstractValidator<GetWateringSummaryQuery>
{
    public GetWateringSummaryQueryValidator()
    {
        RuleFor(x => x.From)
            .NotNull()
            .WithMessage("'from' is required.");

        RuleFor(x => x.To)
            .NotNull()
            .WithMessage("'to' is required.")
            .Must((query, to) => query.From is null || to is null || query.From <= to)
            .WithMessage("'from' must be before or equal to 'to'.");
    }
}
