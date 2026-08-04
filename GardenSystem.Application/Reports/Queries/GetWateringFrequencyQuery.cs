using GardenSystem.Application.Reports.Dtos;
using MediatR;

namespace GardenSystem.Application.Reports.Queries;

public sealed record GetWateringFrequencyQuery(Guid PlantId, string? Period) : IRequest<WateringFrequencyResponseDto>;
