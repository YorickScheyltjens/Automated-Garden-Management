using FluentAssertions;
using GardenSystem.Domain;

namespace GardenSystem.Domain.Tests;

public sealed class TelemetryEvaluatorTests
{
    [Fact]
    public void ShouldStartWatering_BelowThresholdAndIdle_ReturnsTrue()
    {
        var shouldStartWatering = TelemetryEvaluator.ShouldStartWatering(
            currentHumidityLevel: 40m,
            idealHumidityLevel: 55,
            isCurrentlyIrrigating: false);

        shouldStartWatering.Should().BeTrue();
    }

    [Fact]
    public void ShouldStartWatering_BelowThresholdButAlreadyIrrigating_ReturnsFalse()
    {
        var shouldStartWatering = TelemetryEvaluator.ShouldStartWatering(
            currentHumidityLevel: 40m,
            idealHumidityLevel: 55,
            isCurrentlyIrrigating: true);

        shouldStartWatering.Should().BeFalse();
    }

    [Theory]
    [InlineData(55, 55)]
    [InlineData(60, 55)]
    public void ShouldStartWatering_AtOrAboveThreshold_ReturnsFalse(decimal currentHumidityLevel, int idealHumidityLevel)
    {
        var shouldStartWatering = TelemetryEvaluator.ShouldStartWatering(
            currentHumidityLevel,
            idealHumidityLevel,
            isCurrentlyIrrigating: false);

        shouldStartWatering.Should().BeFalse();
    }
}
