using GardenSystem.Application.Gardens.Commands;
using GardenSystem.Application.Gardens.Dtos;
using GardenSystem.Application.Gardens.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GardenSystem.Api.Controllers;

[ApiController]
[Route("api/v1/gardens")]
public sealed class GardensController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Lists gardens for the current user.
    /// </summary>
    /// <response code="200">Returns the user's gardens.</response>
    /// <response code="400">The request was invalid.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<GardenResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<GardenResponseDto>>> ListGardens(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ListGardensByUserIdQuery(), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Creates a garden for the current user.
    /// </summary>
    /// <response code="201">Garden created successfully.</response>
    /// <response code="400">The request payload failed validation.</response>
    [HttpPost]
    [ProducesResponseType(typeof(GardenResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<GardenResponseDto>> CreateGarden(
        [FromBody] CreateGardenRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new CreateGardenCommand(
            request.GardenName,
            request.TotalSurfaceArea,
            request.LocationDescription,
            request.Latitude,
            request.Longitude,
            request.TargetHumidityLevel);

        var result = await mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetGardenById), new { id = result.GardenId }, result);
    }

    /// <summary>
    /// Gets a garden by id for the current user.
    /// </summary>
    /// <response code="200">Garden found.</response>
    /// <response code="400">The request was invalid.</response>
    /// <response code="404">The garden was not found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(GardenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GardenResponseDto>> GetGardenById(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetGardenByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Updates a garden owned by the current user.
    /// </summary>
    /// <response code="200">Garden updated successfully.</response>
    /// <response code="400">The request payload failed validation.</response>
    /// <response code="404">The garden was not found.</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(GardenResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GardenResponseDto>> UpdateGarden(
        Guid id,
        [FromBody] UpdateGardenRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateGardenCommand(
            id,
            request.GardenName,
            request.TotalSurfaceArea,
            request.LocationDescription,
            request.Latitude,
            request.Longitude,
            request.TargetHumidityLevel);

        var result = await mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Soft-deletes a garden owned by the current user.
    /// </summary>
    /// <response code="204">Garden deleted successfully.</response>
    /// <response code="400">The request was invalid.</response>
    /// <response code="404">The garden was not found.</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteGarden(Guid id, CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteGardenCommand(id), cancellationToken);
        return NoContent();
    }
}
