using GardenSystem.Application.Auth.Dtos;
using MediatR;

namespace GardenSystem.Application.Auth.Commands;

public sealed record RegisterUserCommand(string FirstName, string LastName, string Email, string Password) : IRequest<UserResponseDto>;
