using GardenSystem.Application.Abstractions;
using GardenSystem.Application.Auth.Dtos;
using GardenSystem.Application.Repositories;
using GardenSystem.Domain.Entities;
using GardenSystem.Domain.Exceptions;
using MediatR;

namespace GardenSystem.Application.Auth.Commands;

public sealed class RegisterUserCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher) : IRequestHandler<RegisterUserCommand, UserResponseDto>
{
    public async Task<UserResponseDto> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var existingUser = await userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingUser is not null)
        {
            throw new ConflictException($"A user with email '{request.Email}' already exists.");
        }

        var user = new User
        {
            UserId = Guid.NewGuid(),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            PasswordHash = passwordHasher.Hash(request.Password),
            EmailVerified = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        await userRepository.AddAsync(user, cancellationToken);

        return user.ToResponseDto();
    }
}
