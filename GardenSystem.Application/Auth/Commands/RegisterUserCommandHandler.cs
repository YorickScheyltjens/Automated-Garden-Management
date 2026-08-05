using System.Security.Cryptography;
using GardenSystem.Application.Abstractions;
using GardenSystem.Application.Auth.Dtos;
using GardenSystem.Application.Repositories;
using GardenSystem.Domain.Entities;
using GardenSystem.Domain.Exceptions;
using MediatR;

namespace GardenSystem.Application.Auth.Commands;

public sealed class RegisterUserCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IEmailSender emailSender) : IRequestHandler<RegisterUserCommand, UserResponseDto>
{
    private static readonly TimeSpan VerificationCodeLifetime = TimeSpan.FromMinutes(15);

    public async Task<UserResponseDto> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var existingUser = await userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingUser is not null)
        {
            throw new ConflictException($"A user with email '{request.Email}' already exists.");
        }

        var verificationCode = GenerateVerificationCode();

        var user = new User
        {
            UserId = Guid.NewGuid(),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            PasswordHash = passwordHasher.Hash(request.Password),
            EmailVerified = false,
            EmailVerificationCodeHash = passwordHasher.Hash(verificationCode),
            EmailVerificationCodeExpiresAtUtc = DateTime.UtcNow.Add(VerificationCodeLifetime),
            CreatedAtUtc = DateTime.UtcNow
        };

        await userRepository.AddAsync(user, cancellationToken);

        await emailSender.SendEmailAsync(
            user.Email,
            "Verify your GardenSystem email address",
            $"Your verification code is {verificationCode}. It expires in 15 minutes.",
            cancellationToken);

        return user.ToResponseDto();
    }

    private static string GenerateVerificationCode() => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
}
