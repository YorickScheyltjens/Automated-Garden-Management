namespace GardenSystem.Application.Auth.Dtos;

/// <summary>
/// Login request payload.
/// </summary>
public sealed class LoginRequestDto
{
    /// <summary>
    /// Email address.
    /// </summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Plaintext password.
    /// </summary>
    public string Password { get; init; } = string.Empty;
}
