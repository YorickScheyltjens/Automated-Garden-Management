using GardenSystem.Application.Abstractions;
using GardenSystem.Application.Repositories;
using GardenSystem.Domain.Exceptions;
using MediatR;

namespace GardenSystem.Application.Plants.Queries;

public sealed class ListPlantsByGardenIdQueryHandler(
    IPlantRepository plantRepository,
    IGardenRepository gardenRepository,
    ICurrentUserProvider currentUserProvider) : IRequestHandler<ListPlantsByGardenIdQuery, IReadOnlyList<Dtos.PlantResponseDto>>
{
    public async Task<IReadOnlyList<Dtos.PlantResponseDto>> Handle(ListPlantsByGardenIdQuery request, CancellationToken cancellationToken)
    {
        var garden = await gardenRepository.GetByIdAsync(request.GardenId, cancellationToken);
        if (garden is null || garden.UserId != currentUserProvider.GetCurrentUserId())
        {
            throw new NotFoundException($"Garden with id '{request.GardenId}' was not found.");
        }

        var plants = await plantRepository.ListPageByGardenIdAsync(request.GardenId, request.Skip, request.Take, cancellationToken);

        return plants
            .Select(plant => plant.ToResponseDto())
            .ToList();
    }
}
