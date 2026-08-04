using GardenSystem.Application.Abstractions;
using GardenSystem.Application.Reports.Queries;
using GardenSystem.Application.Repositories;
using GardenSystem.Domain.Entities;
using GardenSystem.Domain.Enums;
using GardenSystem.Domain.Exceptions;
using Moq;

namespace GardenSystem.Application.Tests;

public sealed class ReportsHandlersTests
{
    private static readonly Guid CurrentUserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public async Task GetWateringSummaryQueryHandler_ReturnsCountsFromRepository()
    {
        var reportingRepositoryMock = new Mock<IReportingRepository>();
        var currentUserProviderMock = BuildCurrentUserProviderMock(CurrentUserId);

        reportingRepositoryMock
            .Setup(x => x.GetWateringSummaryAsync(CurrentUserId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((3, 7));

        var handler = new GetWateringSummaryQueryHandler(reportingRepositoryMock.Object, currentUserProviderMock.Object);
        var query = new GetWateringSummaryQuery(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.Equal(3, result.WateredCount);
        Assert.Equal(7, result.UnwateredCount);
    }

    [Fact]
    public async Task GetWateringFrequencyQueryHandler_WhenPlantDoesNotExist_ThrowsNotFound()
    {
        var plantRepositoryMock = new Mock<IPlantRepository>();
        var gardenRepositoryMock = new Mock<IGardenRepository>();
        var reportingRepositoryMock = new Mock<IReportingRepository>();
        var currentUserProviderMock = BuildCurrentUserProviderMock(CurrentUserId);

        plantRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Plant?)null);

        var handler = new GetWateringFrequencyQueryHandler(
            plantRepositoryMock.Object, gardenRepositoryMock.Object, reportingRepositoryMock.Object, currentUserProviderMock.Object);

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new GetWateringFrequencyQuery(Guid.NewGuid(), "30m"), CancellationToken.None));
    }

    [Fact]
    public async Task GetWateringFrequencyQueryHandler_WhenPlantOwnedByDifferentUser_ThrowsNotFound()
    {
        var plantId = Guid.NewGuid();
        var gardenId = Guid.NewGuid();

        var plantRepositoryMock = new Mock<IPlantRepository>();
        var gardenRepositoryMock = new Mock<IGardenRepository>();
        var reportingRepositoryMock = new Mock<IReportingRepository>();
        var currentUserProviderMock = BuildCurrentUserProviderMock(CurrentUserId);

        plantRepositoryMock
            .Setup(x => x.GetByIdAsync(plantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildPlant(plantId, gardenId));

        gardenRepositoryMock
            .Setup(x => x.GetByIdAsync(gardenId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildGarden(gardenId, Guid.NewGuid()));

        var handler = new GetWateringFrequencyQueryHandler(
            plantRepositoryMock.Object, gardenRepositoryMock.Object, reportingRepositoryMock.Object, currentUserProviderMock.Object);

        await Assert.ThrowsAsync<NotFoundException>(
            () => handler.Handle(new GetWateringFrequencyQuery(plantId, "30m"), CancellationToken.None));
    }

    [Fact]
    public async Task GetWateringFrequencyQueryHandler_ParsesPeriodAndMapsEvents()
    {
        var plantId = Guid.NewGuid();
        var gardenId = Guid.NewGuid();

        var plantRepositoryMock = new Mock<IPlantRepository>();
        var gardenRepositoryMock = new Mock<IGardenRepository>();
        var reportingRepositoryMock = new Mock<IReportingRepository>();
        var currentUserProviderMock = BuildCurrentUserProviderMock(CurrentUserId);

        plantRepositoryMock
            .Setup(x => x.GetByIdAsync(plantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildPlant(plantId, gardenId));

        gardenRepositoryMock
            .Setup(x => x.GetByIdAsync(gardenId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildGarden(gardenId, CurrentUserId));

        var events = new List<IrrigationEvent>
        {
            new()
            {
                IrrigationEventId = Guid.NewGuid(),
                PlantId = plantId,
                StartTimeUtc = DateTime.UtcNow,
                HumidityBefore = 40m
            }
        };

        DateTime? capturedPeriodStart = null;
        reportingRepositoryMock
            .Setup(x => x.GetIrrigationEventsSinceAsync(plantId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, DateTime, CancellationToken>((_, periodStart, _) => capturedPeriodStart = periodStart)
            .ReturnsAsync(events);

        var handler = new GetWateringFrequencyQueryHandler(
            plantRepositoryMock.Object, gardenRepositoryMock.Object, reportingRepositoryMock.Object, currentUserProviderMock.Object);

        var before = DateTime.UtcNow;
        var result = await handler.Handle(new GetWateringFrequencyQuery(plantId, "30m"), CancellationToken.None);
        var after = DateTime.UtcNow;

        Assert.Equal(plantId, result.PlantId);
        Assert.Equal(1, result.EventCount);
        Assert.Single(result.Events);
        Assert.NotNull(capturedPeriodStart);
        Assert.InRange(capturedPeriodStart!.Value, before.AddMinutes(-30).AddSeconds(-2), after.AddMinutes(-30).AddSeconds(2));
    }

    [Fact]
    public async Task GetPlantChangesQueryHandler_ReturnsCountsFromRepository()
    {
        var reportingRepositoryMock = new Mock<IReportingRepository>();
        var currentUserProviderMock = BuildCurrentUserProviderMock(CurrentUserId);

        reportingRepositoryMock
            .Setup(x => x.GetPlantChangesAsync(CurrentUserId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((5, 2));

        var handler = new GetPlantChangesQueryHandler(reportingRepositoryMock.Object, currentUserProviderMock.Object);
        var result = await handler.Handle(new GetPlantChangesQuery(DateTime.UtcNow.AddDays(-7)), CancellationToken.None);

        Assert.Equal(5, result.Added);
        Assert.Equal(2, result.Deleted);
    }

    private static Plant BuildPlant(Guid plantId, Guid gardenId)
    {
        return new Plant
        {
            PlantId = plantId,
            GardenId = gardenId,
            PlantName = "Test Plant",
            Species = "Test Species",
            PlantType = PlantType.Vegetable,
            PlantationDate = DateOnly.FromDateTime(DateTime.UtcNow),
            SurfaceAreaRequired = 1m,
            IdealHumidityLevel = 55,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    private static Garden BuildGarden(Guid gardenId, Guid userId)
    {
        return new Garden
        {
            GardenId = gardenId,
            UserId = userId,
            GardenName = "Test Garden",
            TotalSurfaceArea = 10m,
            LocationDescription = "Test Plot",
            TargetHumidityLevel = 60,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    private static Mock<ICurrentUserProvider> BuildCurrentUserProviderMock(Guid userId)
    {
        var currentUserProviderMock = new Mock<ICurrentUserProvider>();
        currentUserProviderMock
            .Setup(x => x.GetCurrentUserId())
            .Returns(userId);

        return currentUserProviderMock;
    }
}
