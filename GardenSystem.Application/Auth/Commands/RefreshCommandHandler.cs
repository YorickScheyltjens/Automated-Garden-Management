using GardenSystem.Application.Abstractions;
using GardenSystem.Application.Auth.Dtos;
using GardenSystem.Application.Repositories;
using GardenSystem.Domain.Exceptions;
using MediatR;

namespace GardenSystem.Application.Auth.Commands;

public sealed class RefreshCommandHandler(
    IUserRepository userRepository,
    IJwtTokenGenerator jwtTokenGenerator) : IRequestHandler<RefreshCommand, AuthTokensResponseDto>
{
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(7);

    public async Task<AuthTokensResponseDto> Handle(RefreshCommand request, CancellationToken cancellationToken)
    {
        var refreshTokenHash = RefreshTokenHasher.Hash(request.RefreshToken);
        var user = await userRepository.GetByRefreshTokenHashAsync(refreshTokenHash, cancellationToken);

        if (user is null || user.RefreshTokenExpiresAtUtc is null || user.RefreshTokenExpiresAtUtc < DateTime.UtcNow)
        {
            throw new AuthenticationException("Refresh token is invalid or has expired.");
        }

        var accessToken = jwtTokenGenerator.GenerateAccessToken(user.UserId, user.Email);
        var newRefreshToken = OpaqueTokenGenerator.Generate();

        user.RefreshTokenHash = RefreshTokenHasher.Hash(newRefreshToken);
        user.RefreshTokenExpiresAtUtc = DateTime.UtcNow.Add(RefreshTokenLifetime);

        await userRepository.UpdateAsync(user, cancellationToken);

        return new AuthTokensResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken
        };
    }
}
