using GardenSystem.Application.Auth.Commands;
using GardenSystem.Application.Auth.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GardenSystem.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Registers a new user.
    /// </summary>
    /// <response code="201">User registered successfully.</response>
    /// <response code="400">The request payload failed validation.</response>
    /// <response code="409">A user with this email already exists.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponseDto>> Register(
        [FromBody] RegisterUserRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new RegisterUserCommand(request.FirstName, request.LastName, request.Email, request.Password);
        var result = await mediator.Send(command, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Verifies a user's email address using the code sent at registration.
    /// </summary>
    /// <response code="200">Email verified successfully.</response>
    /// <response code="400">The request payload failed validation, or the code is invalid or expired.</response>
    [HttpPost("verify-email")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyEmail(
        [FromBody] VerifyEmailRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new VerifyEmailCommand(request.Email, request.Code);
        await mediator.Send(command, cancellationToken);

        return Ok();
    }

    /// <summary>
    /// Logs in a verified user and issues an access and refresh token.
    /// </summary>
    /// <response code="200">Login succeeded.</response>
    /// <response code="400">The request payload failed validation.</response>
    /// <response code="401">The credentials are invalid, or the email address is not verified.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthTokensResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthTokensResponseDto>> Login(
        [FromBody] LoginRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new LoginCommand(request.Email, request.Password);
        var result = await mediator.Send(command, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Exchanges a valid refresh token for a new access and refresh token pair.
    /// </summary>
    /// <response code="200">Refresh succeeded.</response>
    /// <response code="400">The request payload failed validation.</response>
    /// <response code="401">The refresh token is invalid or has expired.</response>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthTokensResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthTokensResponseDto>> Refresh(
        [FromBody] RefreshRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new RefreshCommand(request.RefreshToken);
        var result = await mediator.Send(command, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Soft-deletes the current user's account, cascading to their gardens and plants.
    /// </summary>
    /// <response code="204">Account deleted successfully.</response>
    /// <response code="401">No valid access token was provided.</response>
    [Authorize]
    [HttpDelete("me")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteMe(CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteMeCommand(), cancellationToken);

        return NoContent();
    }
}
