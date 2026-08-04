using GardenSystem.Application.Auth.Commands;
using GardenSystem.Application.Auth.Dtos;
using MediatR;
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
}
