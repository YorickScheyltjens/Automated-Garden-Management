using GardenSystem.Application.Auth.Dtos;
using MediatR;

namespace GardenSystem.Application.Auth.Commands;

public sealed record RefreshCommand(string RefreshToken) : IRequest<AuthTokensResponseDto>;
