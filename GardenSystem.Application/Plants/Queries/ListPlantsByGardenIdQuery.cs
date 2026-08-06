using GardenSystem.Application.Plants.Dtos;
using MediatR;

namespace GardenSystem.Application.Plants.Queries;

public sealed record ListPlantsByGardenIdQuery(Guid GardenId, int Skip, int Take) : IRequest<IReadOnlyList<PlantResponseDto>>;
