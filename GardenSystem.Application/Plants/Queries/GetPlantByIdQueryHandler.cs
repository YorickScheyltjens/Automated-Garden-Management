using GardenSystem.Application.Abstractions;
using GardenSystem.Application.Caching;
using GardenSystem.Application.Repositories;
using GardenSystem.Domain.Entities;
using GardenSystem.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Caching.Memory;

namespace GardenSystem.Application.Plants.Queries;

public sealed class GetPlantByIdQueryHandler(
    IPlantRepository plantRepository,
    IGardenRepository gardenRepository,
    ICurrentUserProvider currentUserProvider,
    IMemoryCache cache) : IRequestHandler<GetPlantByIdQuery, Dtos.PlantResponseDto>
{
    public async Task<Dtos.PlantResponseDto> Handle(GetPlantByIdQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeys.Plant(request.PlantId);

        if (!cache.TryGetValue(cacheKey, out Plant? plant))
        {
            plant = await plantRepository.GetByIdAsync(request.PlantId, cancellationToken);

            if (plant is not null)
            {
                cache.Set(cacheKey, plant, CacheKeys.Ttl);
            }
        }

        if (plant is null)
        {
            throw new NotFoundException($"Plant with id '{request.PlantId}' was not found.");
        }

        var garden = await gardenRepository.GetByIdAsync(plant.GardenId, cancellationToken);
        if (garden is null || garden.UserId != currentUserProvider.GetCurrentUserId())
        {
            throw new NotFoundException($"Plant with id '{request.PlantId}' was not found.");
        }

        return plant.ToResponseDto();
    }
}
