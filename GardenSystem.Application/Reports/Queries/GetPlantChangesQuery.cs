using GardenSystem.Application.Reports.Dtos;
using MediatR;

namespace GardenSystem.Application.Reports.Queries;

public sealed record GetPlantChangesQuery(DateTime? Since) : IRequest<PlantChangesResponseDto>;
