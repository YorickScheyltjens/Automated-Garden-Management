namespace GardenSystem.Application.Auth.Dtos;

/// <summary>
/// Registration request payload.
/// </summary>
public sealed class RegisterUserRequestDto
{
    /// <summary>
    /// First name of the user.
    /// </summary>
    public string FirstName { get; init; } = string.Empty;

    /// <summary>
    /// Last name of the user.
    /// </summary>
    public string LastName { get; init; } = string.Empty;

    /// <summary>
    /// Email address, used as the login identifier.
    /// </summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Plaintext password, hashed before storage.
    /// </summary>
    public string Password { get; init; } = string.Empty;
}
