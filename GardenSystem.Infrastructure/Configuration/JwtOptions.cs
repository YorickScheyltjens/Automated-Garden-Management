namespace GardenSystem.Infrastructure.Configuration;

public sealed class JwtOptions
{
    public string Secret { get; set; } = string.Empty;
    public int AccessTokenLifetimeMinutes { get; set; } = 15;
}
