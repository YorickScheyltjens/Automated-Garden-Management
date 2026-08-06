using GardenSystem.Application.Abstractions;
using GardenSystem.Application.Caching;
using GardenSystem.Application.Repositories;
using GardenSystem.Domain.Entities;
using GardenSystem.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Caching.Memory;

namespace GardenSystem.Application.Gardens.Queries;

public sealed class GetGardenByIdQueryHandler(
    IGardenRepository gardenRepository,
    ICurrentUserProvider currentUserProvider,
    IMemoryCache cache) : IRequestHandler<GetGardenByIdQuery, Dtos.GardenResponseDto>
{
    public async Task<Dtos.GardenResponseDto> Handle(GetGardenByIdQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeys.Garden(request.GardenId);

        if (!cache.TryGetValue(cacheKey, out Garden? garden))
        {
            garden = await gardenRepository.GetByIdAsync(request.GardenId, cancellationToken);

            if (garden is not null)
            {
                cache.Set(cacheKey, garden, CacheKeys.Ttl);
            }
        }

        if (garden is null || garden.UserId != currentUserProvider.GetCurrentUserId())
        {
            throw new NotFoundException($"Garden with id '{request.GardenId}' was not found.");
        }

        return garden.ToResponseDto();
    }
}
