using GardenSystem.Application.Abstractions;
using GardenSystem.Application.Repositories;
using MediatR;

namespace GardenSystem.Application.Auth.Commands;

public sealed class DeleteMeCommandHandler(
    IUserRepository userRepository,
    IGardenRepository gardenRepository,
    IPlantRepository plantRepository,
    ICurrentUserProvider currentUserProvider) : IRequestHandler<DeleteMeCommand, Unit>
{
    public async Task<Unit> Handle(DeleteMeCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserProvider.GetCurrentUserId();

        var gardens = await gardenRepository.ListByUserIdAsync(userId, cancellationToken);

        foreach (var garden in gardens)
        {
            var plants = await plantRepository.ListByGardenIdAsync(garden.GardenId, cancellationToken);

            foreach (var plant in plants)
            {
                await plantRepository.SoftDeleteAsync(plant.PlantId, cancellationToken);
            }

            await gardenRepository.SoftDeleteAsync(garden.GardenId, cancellationToken);
        }

        await userRepository.SoftDeleteAsync(userId, cancellationToken);

        return Unit.Value;
    }
}
