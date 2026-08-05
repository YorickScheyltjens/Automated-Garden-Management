namespace GardenSystem.Application.Auth.Dtos;

/// <summary>
/// Refresh token request payload.
/// </summary>
public sealed class RefreshRequestDto
{
    /// <summary>
    /// Opaque refresh token previously issued to the client.
    /// </summary>
    public string RefreshToken { get; init; } = string.Empty;
}
