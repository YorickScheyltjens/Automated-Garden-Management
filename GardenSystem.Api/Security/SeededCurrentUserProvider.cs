using GardenSystem.Application.Abstractions;

namespace GardenSystem.Api.Security;

public sealed class SeededCurrentUserProvider : ICurrentUserProvider
{
    private readonly Guid _currentUserId;

    public SeededCurrentUserProvider(IConfiguration configuration)
    {
        var configuredUserId = configuration["InterimCurrentUser:UserId"];
        if (!Guid.TryParse(configuredUserId, out var parsedUserId))
        {
            throw new InvalidOperationException(
                "Configuration key 'InterimCurrentUser:UserId' must contain a valid Guid.");
        }

        _currentUserId = parsedUserId;
    }

    public Guid GetCurrentUserId()
    {
        return _currentUserId;
    }
}
