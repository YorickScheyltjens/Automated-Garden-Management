using MediatR;

namespace GardenSystem.Application.Auth.Commands;

public sealed record VerifyEmailCommand(string Email, string Code) : IRequest<Unit>;
