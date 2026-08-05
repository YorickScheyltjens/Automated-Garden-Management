namespace GardenSystem.Application.Abstractions;

public interface IJwtTokenGenerator
{
    string GenerateAccessToken(Guid userId, string email);
}
