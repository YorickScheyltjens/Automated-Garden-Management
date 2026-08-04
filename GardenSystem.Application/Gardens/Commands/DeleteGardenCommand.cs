using MediatR;

namespace GardenSystem.Application.Gardens.Commands;

public sealed record DeleteGardenCommand(Guid GardenId) : IRequest<Unit>;
