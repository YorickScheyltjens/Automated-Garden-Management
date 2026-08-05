using GardenSystem.Application.Abstractions;
using GardenSystem.Application.Auth.Dtos;
using GardenSystem.Application.Repositories;
using GardenSystem.Domain.Exceptions;
using MediatR;

namespace GardenSystem.Application.Auth.Commands;

public sealed class LoginCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator) : IRequestHandler<LoginCommand, AuthTokensResponseDto>
{
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(7);

    public async Task<AuthTokensResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new AuthenticationException("Invalid email or password.");
        }

        if (!user.EmailVerified)
        {
            throw new AuthenticationException("Email address is not verified.");
        }

        var accessToken = jwtTokenGenerator.GenerateAccessToken(user.UserId, user.Email);
        var refreshToken = OpaqueTokenGenerator.Generate();

        user.RefreshTokenHash = RefreshTokenHasher.Hash(refreshToken);
        user.RefreshTokenExpiresAtUtc = DateTime.UtcNow.Add(RefreshTokenLifetime);

        await userRepository.UpdateAsync(user, cancellationToken);

        return new AuthTokensResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken
        };
    }
}
