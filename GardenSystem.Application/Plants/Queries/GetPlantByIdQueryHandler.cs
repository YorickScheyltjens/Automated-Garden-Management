using GardenSystem.Application.Abstractions;
using GardenSystem.Application.Repositories;
using GardenSystem.Domain.Exceptions;
using MediatR;

namespace GardenSystem.Application.Plants.Queries;

public sealed class GetPlantByIdQueryHandler(
    IPlantRepository plantRepository,
    IGardenRepository gardenRepository,
    ICurrentUserProvider currentUserProvider) : IRequestHandler<GetPlantByIdQuery, Dtos.PlantResponseDto>
{
    public async Task<Dtos.PlantResponseDto> Handle(GetPlantByIdQuery request, CancellationToken cancellationToken)
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

        return plant.ToResponseDto();
    }
}
