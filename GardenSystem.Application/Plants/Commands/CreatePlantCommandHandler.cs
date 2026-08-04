using GardenSystem.Application.Abstractions;
using GardenSystem.Application.Repositories;
using GardenSystem.Domain.Entities;
using GardenSystem.Domain.Exceptions;
using MediatR;

namespace GardenSystem.Application.Plants.Commands;

public sealed class CreatePlantCommandHandler(
    IPlantRepository plantRepository,
    IGardenRepository gardenRepository,
    ICurrentUserProvider currentUserProvider) : IRequestHandler<CreatePlantCommand, Dtos.PlantResponseDto>
{
    public async Task<Dtos.PlantResponseDto> Handle(CreatePlantCommand request, CancellationToken cancellationToken)
    {
        var garden = await gardenRepository.GetByIdAsync(request.GardenId, cancellationToken);

        if (garden is null || garden.UserId != currentUserProvider.GetCurrentUserId())
        {
            throw new NotFoundException($"Garden with id '{request.GardenId}' was not found.");
        }

        var plant = new Plant
        {
            PlantId = Guid.NewGuid(),
            GardenId = request.GardenId,
            PlantName = request.PlantName,
            Species = request.Species,
            PlantType = request.PlantType,
            PlantationDate = request.PlantationDate,
            SurfaceAreaRequired = request.SurfaceAreaRequired,
            IdealHumidityLevel = request.IdealHumidityLevel,
            CreatedAtUtc = DateTime.UtcNow
        };

        await plantRepository.AddAsync(plant, cancellationToken);

        return plant.ToResponseDto();
    }
}
