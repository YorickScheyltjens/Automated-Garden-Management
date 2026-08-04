using GardenSystem.Application.Abstractions;
using GardenSystem.Application.Repositories;
using GardenSystem.Domain.Exceptions;
using MediatR;

namespace GardenSystem.Application.Gardens.Commands;

public sealed class DeleteGardenCommandHandler(
    IGardenRepository gardenRepository,
    ICurrentUserProvider currentUserProvider) : IRequestHandler<DeleteGardenCommand, Unit>
{
    public async Task<Unit> Handle(DeleteGardenCommand request, CancellationToken cancellationToken)
    {
        var garden = await gardenRepository.GetByIdAsync(request.GardenId, cancellationToken);

        if (garden is null || garden.UserId != currentUserProvider.GetCurrentUserId())
        {
            throw new NotFoundException($"Garden with id '{request.GardenId}' was not found.");
        }

        await gardenRepository.SoftDeleteAsync(request.GardenId, cancellationToken);

        return Unit.Value;
    }
}
