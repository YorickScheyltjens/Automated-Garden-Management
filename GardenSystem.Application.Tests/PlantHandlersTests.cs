using GardenSystem.Application.Abstractions;
using GardenSystem.Application.Plants.Commands;
using GardenSystem.Application.Plants.Queries;
using GardenSystem.Application.Repositories;
using GardenSystem.Domain.Entities;
using GardenSystem.Domain.Enums;
using GardenSystem.Domain.Exceptions;
using Microsoft.Extensions.Caching.Memory;
using Moq;

namespace GardenSystem.Application.Tests;

public sealed class PlantHandlersTests
{
    private static readonly Guid CurrentUserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public async Task CreatePlantCommandHandler_CreatesPlantForExistingOwnedGarden()
    {
        var plantRepositoryMock = new Mock<IPlantRepository>();
        var gardenRepositoryMock = new Mock<IGardenRepository>();
        var currentUserProviderMock = BuildCurrentUserProviderMock(CurrentUserId);
        var gardenId = Guid.NewGuid();

        gardenRepositoryMock
            .Setup(x => x.GetByIdAsync(gardenId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Garden
            {
                GardenId = gardenId,
                UserId = CurrentUserId,
                GardenName = "Garden",
                TotalSurfaceArea = 15m,
                LocationDescription = "Location",
                TargetHumidityLevel = 60,
                CreatedAtUtc = DateTime.UtcNow
            });

        Plant? capturedPlant = null;
        plantRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Plant>(), It.IsAny<CancellationToken>()))
            .Callback<Plant, CancellationToken>((plant, _) => capturedPlant = plant)
            .Returns(Task.CompletedTask);

        var handler = new CreatePlantCommandHandler(
            plantRepositoryMock.Object,
            gardenRepositoryMock.Object,
            currentUserProviderMock.Object);

        var command = new CreatePlantCommand(
            gardenId,
            "Tomato",
            "Solanum lycopersicum",
            PlantType.Vegetable,
            new DateOnly(2026, 8, 4),
            1.2m,
            58);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(capturedPlant);
        Assert.Equal(gardenId, capturedPlant!.GardenId);
        Assert.Equal(58, result.IdealHumidityLevel);
        plantRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Plant>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreatePlantCommandHandler_WhenGardenMissing_ThrowsNotFound()
    {
        var plantRepositoryMock = new Mock<IPlantRepository>();
        var gardenRepositoryMock = new Mock<IGardenRepository>();
        var currentUserProviderMock = BuildCurrentUserProviderMock(CurrentUserId);

        gardenRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Garden?)null);

        var handler = new CreatePlantCommandHandler(
            plantRepositoryMock.Object,
            gardenRepositoryMock.Object,
            currentUserProviderMock.Object);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(
            new CreatePlantCommand(
                Guid.NewGuid(),
                "Tomato",
                "Solanum lycopersicum",
                PlantType.Vegetable,
                new DateOnly(2026, 8, 4),
                1.2m,
                58),
            CancellationToken.None));
    }

    [Fact]
    public async Task GetPlantByIdQueryHandler_WhenPlantMissing_ThrowsNotFound()
    {
        var plantRepositoryMock = new Mock<IPlantRepository>();
        var gardenRepositoryMock = new Mock<IGardenRepository>();
        var currentUserProviderMock = BuildCurrentUserProviderMock(CurrentUserId);

        plantRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Plant?)null);

        var handler = new GetPlantByIdQueryHandler(
            plantRepositoryMock.Object,
            gardenRepositoryMock.Object,
            currentUserProviderMock.Object,
            BuildCache());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new GetPlantByIdQuery(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task UpdatePlantCommandHandler_UpdatesPlant()
    {
        var plantRepositoryMock = new Mock<IPlantRepository>();
        var gardenRepositoryMock = new Mock<IGardenRepository>();
        var currentUserProviderMock = BuildCurrentUserProviderMock(CurrentUserId);
        var plantId = Guid.NewGuid();
        var existingGardenId = Guid.NewGuid();
        var targetGardenId = Guid.NewGuid();

        var existingPlant = new Plant
        {
            PlantId = plantId,
            GardenId = existingGardenId,
            PlantName = "Old",
            Species = "OldSpecies",
            PlantType = PlantType.Flower,
            PlantationDate = new DateOnly(2026, 1, 1),
            SurfaceAreaRequired = 0.8m,
            IdealHumidityLevel = 44,
            CreatedAtUtc = DateTime.UtcNow
        };

        plantRepositoryMock
            .Setup(x => x.GetByIdAsync(plantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPlant);

        gardenRepositoryMock
            .Setup(x => x.GetByIdAsync(existingGardenId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Garden
            {
                GardenId = existingGardenId,
                UserId = CurrentUserId,
                GardenName = "Existing",
                TotalSurfaceArea = 12m,
                LocationDescription = "Loc",
                TargetHumidityLevel = 60,
                CreatedAtUtc = DateTime.UtcNow
            });

        gardenRepositoryMock
            .Setup(x => x.GetByIdAsync(targetGardenId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Garden
            {
                GardenId = targetGardenId,
                UserId = CurrentUserId,
                GardenName = "Target",
                TotalSurfaceArea = 20m,
                LocationDescription = "Loc",
                TargetHumidityLevel = 60,
                CreatedAtUtc = DateTime.UtcNow
            });

        plantRepositoryMock
            .Setup(x => x.UpdateAsync(existingPlant, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new UpdatePlantCommandHandler(
            plantRepositoryMock.Object,
            gardenRepositoryMock.Object,
            currentUserProviderMock.Object,
            BuildCache());

        var result = await handler.Handle(
            new UpdatePlantCommand(
                plantId,
                targetGardenId,
                "NewName",
                "NewSpecies",
                PlantType.Fruit,
                new DateOnly(2026, 8, 4),
                1.7m,
                62),
            CancellationToken.None);

        Assert.Equal(targetGardenId, existingPlant.GardenId);
        Assert.Equal("NewName", existingPlant.PlantName);
        Assert.Equal(62, result.IdealHumidityLevel);

        plantRepositoryMock.Verify(x => x.UpdateAsync(existingPlant, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeletePlantCommandHandler_CallsSoftDeleteForOwnedPlant()
    {
        var plantRepositoryMock = new Mock<IPlantRepository>();
        var gardenRepositoryMock = new Mock<IGardenRepository>();
        var currentUserProviderMock = BuildCurrentUserProviderMock(CurrentUserId);
        var plantId = Guid.NewGuid();
        var gardenId = Guid.NewGuid();

        plantRepositoryMock
            .Setup(x => x.GetByIdAsync(plantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Plant
            {
                PlantId = plantId,
                GardenId = gardenId,
                PlantName = "Plant",
                Species = "Species",
                PlantType = PlantType.Vegetable,
                PlantationDate = new DateOnly(2026, 8, 4),
                SurfaceAreaRequired = 1.1m,
                IdealHumidityLevel = 60,
                CreatedAtUtc = DateTime.UtcNow
            });

        gardenRepositoryMock
            .Setup(x => x.GetByIdAsync(gardenId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Garden
            {
                GardenId = gardenId,
                UserId = CurrentUserId,
                GardenName = "Garden",
                TotalSurfaceArea = 10m,
                LocationDescription = "Location",
                TargetHumidityLevel = 60,
                CreatedAtUtc = DateTime.UtcNow
            });

        plantRepositoryMock
            .Setup(x => x.SoftDeleteAsync(plantId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new DeletePlantCommandHandler(
            plantRepositoryMock.Object,
            gardenRepositoryMock.Object,
            currentUserProviderMock.Object,
            BuildCache());

        var result = await handler.Handle(new DeletePlantCommand(plantId), CancellationToken.None);

        Assert.Equal(MediatR.Unit.Value, result);
        plantRepositoryMock.Verify(x => x.SoftDeleteAsync(plantId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListPlantsByGardenIdQueryHandler_WhenGardenMissing_ThrowsNotFound()
    {
        var plantRepositoryMock = new Mock<IPlantRepository>();
        var gardenRepositoryMock = new Mock<IGardenRepository>();
        var currentUserProviderMock = BuildCurrentUserProviderMock(CurrentUserId);

        gardenRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Garden?)null);

        var handler = new ListPlantsByGardenIdQueryHandler(
            plantRepositoryMock.Object,
            gardenRepositoryMock.Object,
            currentUserProviderMock.Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new ListPlantsByGardenIdQuery(Guid.NewGuid(), 0, 20), CancellationToken.None));
    }

    [Fact]
    public async Task CreatePlantCommandHandler_WhenPlantOverflows_ThrowsOvercrowdingException()
    {
        var plantRepositoryMock = new Mock<IPlantRepository>();
        var gardenRepositoryMock = new Mock<IGardenRepository>();
        var currentUserProviderMock = BuildCurrentUserProviderMock(CurrentUserId);
        var gardenId = Guid.NewGuid();

        gardenRepositoryMock
            .Setup(x => x.GetByIdAsync(gardenId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Garden
            {
                GardenId = gardenId,
                UserId = CurrentUserId,
                GardenName = "Garden",
                TotalSurfaceArea = 10m,
                LocationDescription = "Location",
                TargetHumidityLevel = 60,
                CreatedAtUtc = DateTime.UtcNow
            });

        plantRepositoryMock
            .Setup(x => x.ListByGardenIdAsync(gardenId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Plant>
            {
                CreatePlant(gardenId, 9.8m)
            });

        var handler = new CreatePlantCommandHandler(
            plantRepositoryMock.Object,
            gardenRepositoryMock.Object,
            currentUserProviderMock.Object);

        await Assert.ThrowsAsync<OvercrowdingException>(() =>
            handler.Handle(
                new CreatePlantCommand(
                    gardenId,
                    "Tomato",
                    "Solanum lycopersicum",
                    PlantType.Vegetable,
                    new DateOnly(2026, 8, 4),
                    0.3m,
                    58),
                CancellationToken.None));
    }

    [Fact]
    public async Task UpdatePlantCommandHandler_WhenUpsizeOverflows_ThrowsOvercrowdingException()
    {
        var plantRepositoryMock = new Mock<IPlantRepository>();
        var gardenRepositoryMock = new Mock<IGardenRepository>();
        var currentUserProviderMock = BuildCurrentUserProviderMock(CurrentUserId);
        var plantId = Guid.NewGuid();
        var gardenId = Guid.NewGuid();

        var plantToUpdate = CreatePlant(gardenId, 2m, plantId);

        plantRepositoryMock
            .Setup(x => x.GetByIdAsync(plantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plantToUpdate);

        gardenRepositoryMock
            .Setup(x => x.GetByIdAsync(gardenId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Garden
            {
                GardenId = gardenId,
                UserId = CurrentUserId,
                GardenName = "Garden",
                TotalSurfaceArea = 10m,
                LocationDescription = "Location",
                TargetHumidityLevel = 60,
                CreatedAtUtc = DateTime.UtcNow
            });

        plantRepositoryMock
            .Setup(x => x.ListByGardenIdAsync(gardenId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Plant>
            {
                plantToUpdate,
                CreatePlant(gardenId, 8.6m)
            });

        var handler = new UpdatePlantCommandHandler(
            plantRepositoryMock.Object,
            gardenRepositoryMock.Object,
            currentUserProviderMock.Object,
            BuildCache());

        await Assert.ThrowsAsync<OvercrowdingException>(() =>
            handler.Handle(
                new UpdatePlantCommand(
                    plantId,
                    gardenId,
                    "Tomato",
                    "Solanum lycopersicum",
                    PlantType.Vegetable,
                    new DateOnly(2026, 8, 4),
                    1.6m,
                    58),
                CancellationToken.None));
    }

    [Fact]
    public async Task UpdatePlantCommandHandler_WhenUpsizeStillFits_DoesNotThrow()
    {
        var plantRepositoryMock = new Mock<IPlantRepository>();
        var gardenRepositoryMock = new Mock<IGardenRepository>();
        var currentUserProviderMock = BuildCurrentUserProviderMock(CurrentUserId);
        var plantId = Guid.NewGuid();
        var gardenId = Guid.NewGuid();

        var plantToUpdate = CreatePlant(gardenId, 2m, plantId);

        plantRepositoryMock
            .Setup(x => x.GetByIdAsync(plantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(plantToUpdate);

        gardenRepositoryMock
            .Setup(x => x.GetByIdAsync(gardenId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Garden
            {
                GardenId = gardenId,
                UserId = CurrentUserId,
                GardenName = "Garden",
                TotalSurfaceArea = 10m,
                LocationDescription = "Location",
                TargetHumidityLevel = 60,
                CreatedAtUtc = DateTime.UtcNow
            });

        plantRepositoryMock
            .Setup(x => x.ListByGardenIdAsync(gardenId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Plant>
            {
                plantToUpdate,
                CreatePlant(gardenId, 6.7m)
            });

        plantRepositoryMock
            .Setup(x => x.UpdateAsync(plantToUpdate, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new UpdatePlantCommandHandler(
            plantRepositoryMock.Object,
            gardenRepositoryMock.Object,
            currentUserProviderMock.Object,
            BuildCache());

        var exception = await Record.ExceptionAsync(() =>
            handler.Handle(
                new UpdatePlantCommand(
                    plantId,
                    gardenId,
                    "Tomato",
                    "Solanum lycopersicum",
                    PlantType.Vegetable,
                    new DateOnly(2026, 8, 4),
                    3.3m,
                    58),
                CancellationToken.None));

        Assert.Null(exception);
        plantRepositoryMock.Verify(x => x.UpdateAsync(plantToUpdate, It.IsAny<CancellationToken>()), Times.Once);
    }

    private static Plant CreatePlant(Guid gardenId, decimal surfaceAreaRequired, Guid? plantId = null)
    {
        return new Plant
        {
            PlantId = plantId ?? Guid.NewGuid(),
            GardenId = gardenId,
            PlantName = "Plant",
            Species = "Species",
            PlantType = PlantType.Vegetable,
            PlantationDate = new DateOnly(2026, 8, 4),
            SurfaceAreaRequired = surfaceAreaRequired,
            IdealHumidityLevel = 60,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    private static IMemoryCache BuildCache() => new MemoryCache(new MemoryCacheOptions());

    private static Mock<ICurrentUserProvider> BuildCurrentUserProviderMock(Guid userId)
    {
        var currentUserProviderMock = new Mock<ICurrentUserProvider>();
        currentUserProviderMock
            .Setup(x => x.GetCurrentUserId())
            .Returns(userId);

        return currentUserProviderMock;
    }
}
