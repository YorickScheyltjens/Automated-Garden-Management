using GardenSystem.Application.Plants.Commands;
using GardenSystem.Application.Plants.Dtos;
using GardenSystem.Application.Plants.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GardenSystem.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1")]
public sealed class PlantsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Lists plants for a specific garden owned by the current user.
    /// </summary>
    /// <response code="200">Returns the garden's plants.</response>
    /// <response code="400">The request was invalid.</response>
    /// <response code="404">The garden was not found.</response>
    [HttpGet("gardens/{gardenId:guid}/plants")]
    [ProducesResponseType(typeof(IReadOnlyList<PlantResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<PlantResponseDto>>> ListPlantsByGardenId(
        Guid gardenId,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ListPlantsByGardenIdQuery(gardenId), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Creates a plant in a specific garden owned by the current user.
    /// </summary>
    /// <response code="201">Plant created successfully.</response>
    /// <response code="400">The request payload failed validation.</response>
    /// <response code="409">The plant does not fit within the garden's available surface area.</response>
    /// <response code="404">The garden was not found.</response>
    [HttpPost("gardens/{gardenId:guid}/plants")]
    [ProducesResponseType(typeof(PlantResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlantResponseDto>> CreatePlant(
        Guid gardenId,
        [FromBody] CreatePlantRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new CreatePlantCommand(
            gardenId,
            request.PlantName,
            request.Species,
            request.PlantType,
            request.PlantationDate,
            request.SurfaceAreaRequired,
            request.IdealHumidityLevel);

        var result = await mediator.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetPlantById), new { id = result.PlantId }, result);
    }

    /// <summary>
    /// Gets a plant by id for the current user.
    /// </summary>
    /// <response code="200">Plant found.</response>
    /// <response code="400">The request was invalid.</response>
    /// <response code="404">The plant was not found.</response>
    [HttpGet("plants/{id:guid}")]
    [ProducesResponseType(typeof(PlantResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlantResponseDto>> GetPlantById(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetPlantByIdQuery(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Updates a plant owned by the current user.
    /// </summary>
    /// <response code="200">Plant updated successfully.</response>
    /// <response code="400">The request payload failed validation.</response>
    /// <response code="409">The updated plant does not fit within the garden's available surface area.</response>
    /// <response code="404">The plant or garden was not found.</response>
    [HttpPut("plants/{id:guid}")]
    [ProducesResponseType(typeof(PlantResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PlantResponseDto>> UpdatePlant(
        Guid id,
        [FromBody] UpdatePlantRequestDto request,
        CancellationToken cancellationToken)
    {
        var command = new UpdatePlantCommand(
            id,
            request.GardenId,
            request.PlantName,
            request.Species,
            request.PlantType,
            request.PlantationDate,
            request.SurfaceAreaRequired,
            request.IdealHumidityLevel);

        var result = await mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Soft-deletes a plant owned by the current user.
    /// </summary>
    /// <response code="204">Plant deleted successfully.</response>
    /// <response code="400">The request was invalid.</response>
    /// <response code="404">The plant was not found.</response>
    [HttpDelete("plants/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePlant(Guid id, CancellationToken cancellationToken)
    {
        await mediator.Send(new DeletePlantCommand(id), cancellationToken);
        return NoContent();
    }
}
