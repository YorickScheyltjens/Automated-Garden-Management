using GardenSystem.Application.Reports.Queries;
using GardenSystem.Application.Reports.Validators;

namespace GardenSystem.Application.Tests;

public sealed class ReportsValidatorsTests
{
    [Fact]
    public void GetWateringSummaryQueryValidator_WithoutFromOrTo_IsInvalid()
    {
        var validator = new GetWateringSummaryQueryValidator();
        var query = new GetWateringSummaryQuery(null, null);

        var result = validator.Validate(query);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetWateringSummaryQuery.From));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetWateringSummaryQuery.To));
    }

    [Fact]
    public void GetWateringSummaryQueryValidator_WhenFromIsAfterTo_IsInvalid()
    {
        var validator = new GetWateringSummaryQueryValidator();
        var query = new GetWateringSummaryQuery(DateTime.UtcNow, DateTime.UtcNow.AddDays(-1));

        var result = validator.Validate(query);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetWateringSummaryQuery.To));
    }

    [Fact]
    public void GetWateringSummaryQueryValidator_WithValidRange_IsValid()
    {
        var validator = new GetWateringSummaryQueryValidator();
        var query = new GetWateringSummaryQuery(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow);

        var result = validator.Validate(query);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("30m")]
    [InlineData("1h")]
    [InlineData("120m")]
    public void GetWateringFrequencyQueryValidator_WithValidPeriod_IsValid(string period)
    {
        var validator = new GetWateringFrequencyQueryValidator();
        var query = new GetWateringFrequencyQuery(Guid.NewGuid(), period);

        var result = validator.Validate(query);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("30")]
    [InlineData("30minutes")]
    [InlineData("h30")]
    public void GetWateringFrequencyQueryValidator_WithInvalidPeriod_IsInvalid(string? period)
    {
        var validator = new GetWateringFrequencyQueryValidator();
        var query = new GetWateringFrequencyQuery(Guid.NewGuid(), period);

        var result = validator.Validate(query);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetWateringFrequencyQuery.Period));
    }

    [Fact]
    public void GetPlantChangesQueryValidator_WithoutSince_IsInvalid()
    {
        var validator = new GetPlantChangesQueryValidator();
        var query = new GetPlantChangesQuery(null);

        var result = validator.Validate(query);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(GetPlantChangesQuery.Since));
    }

    [Fact]
    public void GetPlantChangesQueryValidator_WithSince_IsValid()
    {
        var validator = new GetPlantChangesQueryValidator();
        var query = new GetPlantChangesQuery(DateTime.UtcNow.AddDays(-30));

        var result = validator.Validate(query);

        Assert.True(result.IsValid);
    }
}
