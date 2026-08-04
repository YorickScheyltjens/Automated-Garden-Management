using GardenSystem.Application.Gardens.Dtos;
using MediatR;

namespace GardenSystem.Application.Gardens.Queries;

public sealed record GetGardenByIdQuery(Guid GardenId) : IRequest<GardenResponseDto>;
