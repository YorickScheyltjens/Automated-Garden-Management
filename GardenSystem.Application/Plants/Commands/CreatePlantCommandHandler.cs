using GardenSystem.Application.Abstractions;
using GardenSystem.Application.Repositories;
using GardenSystem.Domain.Entities;
using GardenSystem.Domain.Exceptions;
using MediatR;
using System.Globalization;

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

        var existingPlants = await plantRepository.ListByGardenIdAsync(request.GardenId, cancellationToken) ?? [];
        if (!garden.CanFitPlant(existingPlants, request.SurfaceAreaRequired))
        {
            var usedSurfaceArea = existingPlants
                .Where(plant => plant.DeletedAtUtc is null)
                .Sum(plant => plant.SurfaceAreaRequired);

            var remainingSurfaceArea = garden.TotalSurfaceArea - usedSurfaceArea;

            throw new OvercrowdingException(
                $"Adding this plant requires {request.SurfaceAreaRequired.ToString("0.##", CultureInfo.InvariantCulture)}m2, " +
                $"but only {remainingSurfaceArea.ToString("0.##", CultureInfo.InvariantCulture)}m2 of the " +
                $"{garden.TotalSurfaceArea.ToString("0.##", CultureInfo.InvariantCulture)}m2 garden remains available.");
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
