namespace GardenSystem.Api.Security;

public sealed class ApiKeyOptions
{
    public string Key { get; set; } = string.Empty;

    public Guid ServiceUserId { get; set; }
}
