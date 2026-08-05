namespace GardenSystem.Application.Auth.Dtos;

/// <summary>
/// Email verification request payload.
/// </summary>
public sealed class VerifyEmailRequestDto
{
    /// <summary>
    /// Email address being verified.
    /// </summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// 6-digit verification code sent to the email address.
    /// </summary>
    public string Code { get; init; } = string.Empty;
}
