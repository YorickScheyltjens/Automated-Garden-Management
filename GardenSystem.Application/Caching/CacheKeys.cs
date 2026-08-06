namespace GardenSystem.Application.Caching;

public static class CacheKeys
{
    public static readonly TimeSpan Ttl = TimeSpan.FromSeconds(30);

    public static string Garden(Guid gardenId) => $"garden:{gardenId}";

    public static string Plant(Guid plantId) => $"plant:{plantId}";
}
