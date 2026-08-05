namespace GardenSystem.Application.Auth.Dtos;

/// <summary>
/// Access and refresh tokens issued on login or refresh.
/// </summary>
public sealed class AuthTokensResponseDto
{
    /// <summary>
    /// Short-lived JWT access token (15 minutes).
    /// </summary>
    public string AccessToken { get; init; } = string.Empty;

    /// <summary>
    /// Long-lived opaque refresh token (7 days), rotated on use.
    /// </summary>
    public string RefreshToken { get; init; } = string.Empty;
}
