using GardenSystem.Application.Auth.Dtos;
using GardenSystem.Domain.Entities;

namespace GardenSystem.Application.Auth;

internal static class UserMappings
{
    public static UserResponseDto ToResponseDto(this User user)
    {
        return new UserResponseDto
        {
            UserId = user.UserId,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            EmailVerified = user.EmailVerified,
            CreatedAtUtc = user.CreatedAtUtc
        };
    }
}
