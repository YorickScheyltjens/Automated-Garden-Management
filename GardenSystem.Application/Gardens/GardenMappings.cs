using GardenSystem.Application.Gardens.Dtos;
using GardenSystem.Domain.Entities;

namespace GardenSystem.Application.Gardens;

internal static class GardenMappings
{
    public static GardenResponseDto ToResponseDto(this Garden garden)
    {
        return new GardenResponseDto
        {
            GardenId = garden.GardenId,
            GardenName = garden.GardenName,
            TotalSurfaceArea = garden.TotalSurfaceArea,
            LocationDescription = garden.LocationDescription,
            Latitude = garden.Latitude,
            Longitude = garden.Longitude,
            TargetHumidityLevel = garden.TargetHumidityLevel,
            CreatedAtUtc = garden.CreatedAtUtc
        };
    }
}
