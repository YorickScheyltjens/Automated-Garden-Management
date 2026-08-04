using GardenSystem.Application.Gardens.Dtos;
using MediatR;

namespace GardenSystem.Application.Gardens.Commands;

public sealed record CreateGardenCommand(
    string GardenName,
    decimal TotalSurfaceArea,
    string LocationDescription,
    decimal? Latitude,
    decimal? Longitude,
    int TargetHumidityLevel) : IRequest<GardenResponseDto>;
