using GardenSystem.Application.Gardens.Dtos;
using MediatR;

namespace GardenSystem.Application.Gardens.Queries;

public sealed record ListGardensByUserIdQuery : IRequest<IReadOnlyList<GardenResponseDto>>;
