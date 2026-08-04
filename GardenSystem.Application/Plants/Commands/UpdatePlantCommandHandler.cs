using GardenSystem.Application.Abstractions;
using GardenSystem.Application.Repositories;
using GardenSystem.Domain.Exceptions;
using MediatR;

namespace GardenSystem.Application.Plants.Commands;

public sealed class UpdatePlantCommandHandler(
    IPlantRepository plantRepository,
    IGardenRepository gardenRepository,
    ICurrentUserProvider currentUserProvider) : IRequestHandler<UpdatePlantCommand, Dtos.PlantResponseDto>
{
    public async Task<Dtos.PlantResponseDto> Handle(UpdatePlantCommand request, CancellationToken cancellationToken)
    {
        var plant = await plantRepository.GetByIdAsync(request.PlantId, cancellationToken);
        if (plant is null)
        {
            throw new NotFoundException($"Plant with id '{request.PlantId}' was not found.");
        }

        var targetGarden = await gardenRepository.GetByIdAsync(request.GardenId, cancellationToken);
        if (targetGarden is null || targetGarden.UserId != currentUserProvider.GetCurrentUserId())
        {
            throw new NotFoundException($"Garden with id '{request.GardenId}' was not found.");
        }

        var existingGarden = await gardenRepository.GetByIdAsync(plant.GardenId, cancellationToken);
        if (existingGarden is null || existingGarden.UserId != currentUserProvider.GetCurrentUserId())
        {
            throw new NotFoundException($"Plant with id '{request.PlantId}' was not found.");
        }

        plant.GardenId = request.GardenId;
        plant.PlantName = request.PlantName;
        plant.Species = request.Species;
        plant.PlantType = request.PlantType;
        plant.PlantationDate = request.PlantationDate;
        plant.SurfaceAreaRequired = request.SurfaceAreaRequired;
        plant.IdealHumidityLevel = request.IdealHumidityLevel;

        await plantRepository.UpdateAsync(plant, cancellationToken);

        return plant.ToResponseDto();
    }
}
