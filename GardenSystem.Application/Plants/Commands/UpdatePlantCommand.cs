using GardenSystem.Application.Plants.Dtos;
using GardenSystem.Domain.Enums;
using MediatR;

namespace GardenSystem.Application.Plants.Commands;

public sealed record UpdatePlantCommand(
    Guid PlantId,
    Guid GardenId,
    string PlantName,
    string Species,
    PlantType PlantType,
    DateOnly PlantationDate,
    decimal SurfaceAreaRequired,
    int IdealHumidityLevel) : IRequest<PlantResponseDto>;
