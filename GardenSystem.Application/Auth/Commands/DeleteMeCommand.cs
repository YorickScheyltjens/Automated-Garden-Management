using MediatR;

namespace GardenSystem.Application.Auth.Commands;

public sealed record DeleteMeCommand : IRequest<Unit>;
