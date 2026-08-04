using FluentAssertions;
using GardenSystem.Domain.Enums;

namespace GardenSystem.Domain.Tests;

public sealed class IrrigationRatesTests
{
    [Theory]
    [InlineData(PlantType.Vegetable, 1)]
    [InlineData(PlantType.Fruit, 3)]
    [InlineData(PlantType.Flower, 4)]
    public void GetDecayRate_ReturnsThePerMinutePercentageForThePlantType(PlantType plantType, decimal expectedDecayRate)
    {
        var decayRate = IrrigationRates.GetDecayRate(plantType);

        decayRate.Should().Be(expectedDecayRate);
    }

    [Theory]
    [InlineData(PlantType.Vegetable, 16)]
    [InlineData(PlantType.Fruit, 18)]
    [InlineData(PlantType.Flower, 20)]
    public void GetRecoveryRate_ReturnsTheFullTwoMinuteWateringPercentageForThePlantType(PlantType plantType, decimal expectedRecoveryRate)
    {
        var recoveryRate = IrrigationRates.GetRecoveryRate(plantType);

        recoveryRate.Should().Be(expectedRecoveryRate);
    }
}
