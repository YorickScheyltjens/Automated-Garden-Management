using GardenSystem.Application.Auth.Dtos;
using MediatR;

namespace GardenSystem.Application.Auth.Commands;

public sealed record LoginCommand(string Email, string Password) : IRequest<AuthTokensResponseDto>;
