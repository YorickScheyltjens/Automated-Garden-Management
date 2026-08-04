using System.Text.RegularExpressions;
using FluentValidation;
using GardenSystem.Application.Reports.Queries;

namespace GardenSystem.Application.Reports.Validators;

public sealed partial class GetWateringFrequencyQueryValidator : AbstractValidator<GetWateringFrequencyQuery>
{
    public GetWateringFrequencyQueryValidator()
    {
        RuleFor(x => x.Period)
            .NotEmpty()
            .WithMessage("'period' is required.")
            .Must(period => period is not null && PeriodPattern().IsMatch(period))
            .WithMessage("'period' must match <number>m or <number>h, e.g. '30m' or '1h'.");
    }

    [GeneratedRegex(@"^\d+[mh]$")]
    private static partial Regex PeriodPattern();
}
