using GardenSystem.Application.Plants.Commands;
using GardenSystem.Application.Plants.Validators;
using GardenSystem.Domain.Enums;

namespace GardenSystem.Application.Tests;

public sealed class PlantValidatorsTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public void CreatePlantCommandValidator_IdealHumidityBoundaries_AreValid(int humidity)
    {
        var validator = new CreatePlantCommandValidator();
        var command = BuildCreateCommand(idealHumidityLevel: humidity);

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void CreatePlantCommandValidator_IdealHumidityOutOfRange_IsInvalid(int humidity)
    {
        var validator = new CreatePlantCommandValidator();
        var command = BuildCreateCommand(idealHumidityLevel: humidity);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreatePlantCommand.IdealHumidityLevel));
    }

    [Fact]
    public void UpdatePlantCommandValidator_WithInvalidPlantType_IsInvalid()
    {
        var validator = new UpdatePlantCommandValidator();
        var command = new UpdatePlantCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Plant",
            "Species",
            (PlantType)999,
            new DateOnly(2026, 8, 4),
            1.0m,
            50);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdatePlantCommand.PlantType));
    }

    [Fact]
    public void CreatePlantCommandValidator_WithMissingNameAndSpecies_IsInvalid()
    {
        var validator = new CreatePlantCommandValidator();
        var command = new CreatePlantCommand(
            Guid.NewGuid(),
            string.Empty,
            string.Empty,
            PlantType.Flower,
            new DateOnly(2026, 8, 4),
            1.0m,
            50);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreatePlantCommand.PlantName));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreatePlantCommand.Species));
    }

    [Fact]
    public void CreatePlantCommandValidator_WithNonPositiveSurfaceArea_IsInvalid()
    {
        var validator = new CreatePlantCommandValidator();
        var command = BuildCreateCommand(idealHumidityLevel: 50) with { SurfaceAreaRequired = 0m };

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreatePlantCommand.SurfaceAreaRequired));
    }

    private static CreatePlantCommand BuildCreateCommand(int idealHumidityLevel)
    {
        return new CreatePlantCommand(
            Guid.NewGuid(),
            "Plant",
            "Species",
            PlantType.Flower,
            new DateOnly(2026, 8, 4),
            1.5m,
            idealHumidityLevel);
    }
}
