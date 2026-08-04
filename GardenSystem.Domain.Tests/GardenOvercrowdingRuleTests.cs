using FluentAssertions;
using GardenSystem.Domain.Entities;

namespace GardenSystem.Domain.Tests;

public sealed class GardenOvercrowdingRuleTests
{
    [Fact]
    public void CanFitPlant_EmptyGarden_PlantFitsComfortably_ReturnsTrue()
    {
        var garden = new Garden { TotalSurfaceArea = 20.00m };
        var existingPlants = new List<Plant>();
        var newPlantSurfaceArea = 5.00m;

        var canFit = garden.CanFitPlant(existingPlants, newPlantSurfaceArea);

        canFit.Should().BeTrue();
    }

    [Fact]
    public void CanFitPlant_WhenTotalIsExactBoundary_ReturnsTrue()
    {
        var garden = new Garden { TotalSurfaceArea = 10.00m };
        var existingPlants = new List<Plant>
        {
            CreatePlant(3.50m),
            CreatePlant(2.50m)
        };
        var newPlantSurfaceArea = 4.00m;

        var canFit = garden.CanFitPlant(existingPlants, newPlantSurfaceArea);

        canFit.Should().BeTrue();
    }

    [Fact]
    public void CanFitPlant_WhenExceedsByPointZeroOne_ReturnsFalse()
    {
        var garden = new Garden { TotalSurfaceArea = 10.00m };
        var existingPlants = new List<Plant>
        {
            CreatePlant(6.00m),
            CreatePlant(3.00m)
        };
        var newPlantSurfaceArea = 1.01m;

        var canFit = garden.CanFitPlant(existingPlants, newPlantSurfaceArea);

        canFit.Should().BeFalse();
    }

    [Fact]
    public void CanFitPlant_WhenExceedsByLargeMargin_ReturnsFalse()
    {
        var garden = new Garden { TotalSurfaceArea = 10.00m };
        var existingPlants = new List<Plant>
        {
            CreatePlant(4.00m),
            CreatePlant(3.00m)
        };
        var newPlantSurfaceArea = 8.00m;

        var canFit = garden.CanFitPlant(existingPlants, newPlantSurfaceArea);

        canFit.Should().BeFalse();
    }

    [Fact]
    public void CanFitPlant_SoftDeletedPlants_AreIgnoredInUsedSurfaceCalculation()
    {
        var garden = new Garden { TotalSurfaceArea = 10.00m };
        var existingPlants = new List<Plant>
        {
            CreatePlant(7.00m),
            CreatePlant(100.00m, deletedAtUtc: DateTime.UtcNow)
        };
        var newPlantSurfaceArea = 3.00m;

        var canFit = garden.CanFitPlant(existingPlants, newPlantSurfaceArea);

        canFit.Should().BeTrue();
    }

    [Fact]
    public void CanFitPlant_ResizingExistingPlantBeyondRemainingSpace_ReturnsFalse()
    {
        var garden = new Garden { TotalSurfaceArea = 10.00m };

        // The plant being resized should be excluded from existingPlants to avoid double-counting itself.
        var existingPlants = new List<Plant>
        {
            CreatePlant(7.50m)
        };

        var resizedPlantSurfaceArea = 2.60m;

        var canFit = garden.CanFitPlant(existingPlants, resizedPlantSurfaceArea);

        canFit.Should().BeFalse();
    }

    private static Plant CreatePlant(decimal surfaceAreaRequired, DateTime? deletedAtUtc = null)
    {
        return new Plant
        {
            PlantId = Guid.NewGuid(),
            GardenId = Guid.NewGuid(),
            PlantName = "Plant",
            Species = "Species",
            SurfaceAreaRequired = surfaceAreaRequired,
            PlantationDate = new DateOnly(2026, 8, 4),
            CreatedAtUtc = DateTime.UtcNow,
            DeletedAtUtc = deletedAtUtc
        };
    }
}
