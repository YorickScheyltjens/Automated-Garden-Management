using GardenSystem.Application.Abstractions;
using GardenSystem.Application.Caching;
using GardenSystem.Application.Repositories;
using GardenSystem.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Caching.Memory;

namespace GardenSystem.Application.Gardens.Commands;

public sealed class DeleteGardenCommandHandler(
    IGardenRepository gardenRepository,
    ICurrentUserProvider currentUserProvider,
    IMemoryCache cache) : IRequestHandler<DeleteGardenCommand, Unit>
{
    public async Task<Unit> Handle(DeleteGardenCommand request, CancellationToken cancellationToken)
    {
        var garden = await gardenRepository.GetByIdAsync(request.GardenId, cancellationToken);

        if (garden is null || garden.UserId != currentUserProvider.GetCurrentUserId())
        {
            throw new NotFoundException($"Garden with id '{request.GardenId}' was not found.");
        }

        await gardenRepository.SoftDeleteAsync(request.GardenId, cancellationToken);
        cache.Remove(CacheKeys.Garden(request.GardenId));

        return Unit.Value;
    }
}
