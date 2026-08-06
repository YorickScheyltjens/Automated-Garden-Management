using GardenSystem.Application.Abstractions;
using GardenSystem.Application.Caching;
using GardenSystem.Application.Repositories;
using GardenSystem.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Caching.Memory;

namespace GardenSystem.Application.Plants.Commands;

public sealed class DeletePlantCommandHandler(
    IPlantRepository plantRepository,
    IGardenRepository gardenRepository,
    ICurrentUserProvider currentUserProvider,
    IMemoryCache cache) : IRequestHandler<DeletePlantCommand, Unit>
{
    public async Task<Unit> Handle(DeletePlantCommand request, CancellationToken cancellationToken)
    {
        var plant = await plantRepository.GetByIdAsync(request.PlantId, cancellationToken);
        if (plant is null)
        {
            throw new NotFoundException($"Plant with id '{request.PlantId}' was not found.");
        }

        var garden = await gardenRepository.GetByIdAsync(plant.GardenId, cancellationToken);
        if (garden is null || garden.UserId != currentUserProvider.GetCurrentUserId())
        {
            throw new NotFoundException($"Plant with id '{request.PlantId}' was not found.");
        }

        await plantRepository.SoftDeleteAsync(request.PlantId, cancellationToken);
        cache.Remove(CacheKeys.Plant(request.PlantId));

        return Unit.Value;
    }
}
