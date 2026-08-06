using GardenSystem.Application.Abstractions;
using GardenSystem.Application.Gardens.Commands;
using GardenSystem.Application.Gardens.Queries;
using GardenSystem.Application.Repositories;
using GardenSystem.Domain.Entities;
using GardenSystem.Domain.Exceptions;
using Microsoft.Extensions.Caching.Memory;
using Moq;

namespace GardenSystem.Application.Tests;

public sealed class GardenHandlersTests
{
    private static readonly Guid CurrentUserId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public async Task CreateGardenCommandHandler_CreatesGardenForCurrentUser()
    {
        var repositoryMock = new Mock<IGardenRepository>();
        var currentUserProviderMock = BuildCurrentUserProviderMock(CurrentUserId);

        Garden? capturedGarden = null;

        repositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Garden>(), It.IsAny<CancellationToken>()))
            .Callback<Garden, CancellationToken>((garden, _) => capturedGarden = garden)
            .Returns(Task.CompletedTask);

        var handler = new CreateGardenCommandHandler(repositoryMock.Object, currentUserProviderMock.Object);
        var command = new CreateGardenCommand("Backyard", 40m, "South side", 51.2194m, 4.4025m, 62);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.NotNull(capturedGarden);
        Assert.Equal(CurrentUserId, capturedGarden!.UserId);
        Assert.Equal(command.TargetHumidityLevel, capturedGarden.TargetHumidityLevel);
        Assert.Equal(capturedGarden.GardenId, result.GardenId);

        repositoryMock.Verify(x => x.AddAsync(It.IsAny<Garden>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateGardenCommandHandler_WhenGardenDoesNotExist_ThrowsNotFound()
    {
        var repositoryMock = new Mock<IGardenRepository>();
        var currentUserProviderMock = BuildCurrentUserProviderMock(CurrentUserId);

        repositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Garden?)null);

        var handler = new UpdateGardenCommandHandler(repositoryMock.Object, currentUserProviderMock.Object, BuildCache());
        var command = new UpdateGardenCommand(Guid.NewGuid(), "Updated", 22m, "Updated", null, null, 55);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateGardenCommandHandler_WhenOwnedByDifferentUser_ThrowsNotFound()
    {
        var repositoryMock = new Mock<IGardenRepository>();
        var currentUserProviderMock = BuildCurrentUserProviderMock(CurrentUserId);

        repositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Garden
            {
                GardenId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                GardenName = "Other User Garden",
                TotalSurfaceArea = 12m,
                LocationDescription = "Other",
                TargetHumidityLevel = 50,
                CreatedAtUtc = DateTime.UtcNow
            });

        var handler = new UpdateGardenCommandHandler(repositoryMock.Object, currentUserProviderMock.Object, BuildCache());
        var command = new UpdateGardenCommand(Guid.NewGuid(), "Updated", 22m, "Updated", null, null, 55);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateGardenCommandHandler_UpdatesGarden()
    {
        var repositoryMock = new Mock<IGardenRepository>();
        var currentUserProviderMock = BuildCurrentUserProviderMock(CurrentUserId);
        var gardenId = Guid.NewGuid();

        var existingGarden = new Garden
        {
            GardenId = gardenId,
            UserId = CurrentUserId,
            GardenName = "Old",
            TotalSurfaceArea = 12m,
            LocationDescription = "Old",
            TargetHumidityLevel = 50,
            CreatedAtUtc = DateTime.UtcNow
        };

        repositoryMock
            .Setup(x => x.GetByIdAsync(gardenId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingGarden);

        repositoryMock
            .Setup(x => x.UpdateAsync(existingGarden, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new UpdateGardenCommandHandler(repositoryMock.Object, currentUserProviderMock.Object, BuildCache());
        var command = new UpdateGardenCommand(gardenId, "New Name", 30m, "North side", 51m, 4m, 70);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Equal("New Name", existingGarden.GardenName);
        Assert.Equal(70, existingGarden.TargetHumidityLevel);
        Assert.Equal(30m, result.TotalSurfaceArea);

        repositoryMock.Verify(x => x.UpdateAsync(existingGarden, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteGardenCommandHandler_WhenGardenDoesNotExist_ThrowsNotFound()
    {
        var repositoryMock = new Mock<IGardenRepository>();
        var currentUserProviderMock = BuildCurrentUserProviderMock(CurrentUserId);

        repositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Garden?)null);

        var handler = new DeleteGardenCommandHandler(repositoryMock.Object, currentUserProviderMock.Object, BuildCache());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new DeleteGardenCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task DeleteGardenCommandHandler_CallsSoftDeleteForOwnedGarden()
    {
        var repositoryMock = new Mock<IGardenRepository>();
        var currentUserProviderMock = BuildCurrentUserProviderMock(CurrentUserId);
        var gardenId = Guid.NewGuid();

        repositoryMock
            .Setup(x => x.GetByIdAsync(gardenId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Garden
            {
                GardenId = gardenId,
                UserId = CurrentUserId,
                GardenName = "Owned Garden",
                TotalSurfaceArea = 14m,
                LocationDescription = "Yard",
                TargetHumidityLevel = 60,
                CreatedAtUtc = DateTime.UtcNow
            });

        repositoryMock
            .Setup(x => x.SoftDeleteAsync(gardenId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new DeleteGardenCommandHandler(repositoryMock.Object, currentUserProviderMock.Object, BuildCache());

        var result = await handler.Handle(new DeleteGardenCommand(gardenId), CancellationToken.None);

        Assert.Equal(MediatR.Unit.Value, result);
        repositoryMock.Verify(x => x.SoftDeleteAsync(gardenId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetGardenByIdQueryHandler_WhenOwnedGardenExists_ReturnsGarden()
    {
        var repositoryMock = new Mock<IGardenRepository>();
        var currentUserProviderMock = BuildCurrentUserProviderMock(CurrentUserId);
        var gardenId = Guid.NewGuid();

        repositoryMock
            .Setup(x => x.GetByIdAsync(gardenId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Garden
            {
                GardenId = gardenId,
                UserId = CurrentUserId,
                GardenName = "Owned",
                TotalSurfaceArea = 20m,
                LocationDescription = "Location",
                TargetHumidityLevel = 63,
                CreatedAtUtc = DateTime.UtcNow
            });

        var handler = new GetGardenByIdQueryHandler(repositoryMock.Object, currentUserProviderMock.Object, BuildCache());

        var result = await handler.Handle(new GetGardenByIdQuery(gardenId), CancellationToken.None);

        Assert.Equal(gardenId, result.GardenId);
        Assert.Equal(63, result.TargetHumidityLevel);
    }

    [Fact]
    public async Task GetGardenByIdQueryHandler_WhenOwnedGardenDoesNotExist_ThrowsNotFound()
    {
        var repositoryMock = new Mock<IGardenRepository>();
        var currentUserProviderMock = BuildCurrentUserProviderMock(CurrentUserId);

        repositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Garden?)null);

        var handler = new GetGardenByIdQueryHandler(repositoryMock.Object, currentUserProviderMock.Object, BuildCache());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new GetGardenByIdQuery(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task ListGardensByUserIdQueryHandler_UsesCurrentUserId()
    {
        var repositoryMock = new Mock<IGardenRepository>();
        var currentUserProviderMock = BuildCurrentUserProviderMock(CurrentUserId);

        repositoryMock
            .Setup(x => x.ListPageByUserIdAsync(CurrentUserId, 0, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Garden>
            {
                new()
                {
                    GardenId = Guid.NewGuid(),
                    UserId = CurrentUserId,
                    GardenName = "One",
                    TotalSurfaceArea = 10m,
                    LocationDescription = "Location",
                    TargetHumidityLevel = 60,
                    CreatedAtUtc = DateTime.UtcNow
                },
                new()
                {
                    GardenId = Guid.NewGuid(),
                    UserId = CurrentUserId,
                    GardenName = "Two",
                    TotalSurfaceArea = 20m,
                    LocationDescription = "Location",
                    TargetHumidityLevel = 61,
                    CreatedAtUtc = DateTime.UtcNow
                }
            });

        var handler = new ListGardensByUserIdQueryHandler(repositoryMock.Object, currentUserProviderMock.Object);

        var result = await handler.Handle(new ListGardensByUserIdQuery(0, 20), CancellationToken.None);

        Assert.Equal(2, result.Count);
        repositoryMock.Verify(x => x.ListPageByUserIdAsync(CurrentUserId, 0, 20, It.IsAny<CancellationToken>()), Times.Once);
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
