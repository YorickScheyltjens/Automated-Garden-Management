namespace GardenSystem.Application.Auth.Dtos;

/// <summary>
/// User details returned by API endpoints. Never includes the password hash.
/// </summary>
public sealed class UserResponseDto
{
    /// <summary>
    /// Unique user identifier.
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// First name of the user.
    /// </summary>
    public string FirstName { get; init; } = string.Empty;

    /// <summary>
    /// Last name of the user.
    /// </summary>
    public string LastName { get; init; } = string.Empty;

    /// <summary>
    /// Email address.
    /// </summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Whether the email address has been verified.
    /// </summary>
    public bool EmailVerified { get; init; }

    /// <summary>
    /// UTC timestamp when the user was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; init; }
}
