using GardenSystem.Application.Plants.Dtos;
using GardenSystem.Domain.Entities;

namespace GardenSystem.Application.Plants;

internal static class PlantMappings
{
    public static PlantResponseDto ToResponseDto(this Plant plant)
    {
        return new PlantResponseDto
        {
            PlantId = plant.PlantId,
            GardenId = plant.GardenId,
            PlantName = plant.PlantName,
            Species = plant.Species,
            PlantType = plant.PlantType,
            PlantationDate = plant.PlantationDate,
            SurfaceAreaRequired = plant.SurfaceAreaRequired,
            IdealHumidityLevel = plant.IdealHumidityLevel,
            CreatedAtUtc = plant.CreatedAtUtc
        };
    }
}
