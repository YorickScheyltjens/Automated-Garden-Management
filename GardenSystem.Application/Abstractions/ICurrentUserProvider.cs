namespace GardenSystem.Application.Abstractions;

public interface ICurrentUserProvider
{
    Guid GetCurrentUserId();
}
