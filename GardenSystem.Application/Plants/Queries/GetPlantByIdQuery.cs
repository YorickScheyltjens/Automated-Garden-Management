using GardenSystem.Application.Plants.Dtos;
using MediatR;

namespace GardenSystem.Application.Plants.Queries;

public sealed record GetPlantByIdQuery(Guid PlantId) : IRequest<PlantResponseDto>;
