using GardenSystem.Application.Reports.Dtos;
using GardenSystem.Application.Reports.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace GardenSystem.Api.Controllers;

[ApiController]
[Route("api/v1/reports")]
public sealed class ReportsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Counts watered vs. unwatered plants for the current user within a period.
    /// </summary>
    /// <response code="200">Returns the watering summary.</response>
    /// <response code="400">The request was invalid.</response>
    [HttpGet("watering-summary")]
    [ProducesResponseType(typeof(WateringSummaryResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<WateringSummaryResponseDto>> GetWateringSummary(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetWateringSummaryQuery(from, to), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Lists irrigation events for a plant owned by the current user within a period.
    /// </summary>
    /// <response code="200">Returns the watering frequency.</response>
    /// <response code="400">The request was invalid.</response>
    /// <response code="404">The plant was not found.</response>
    [HttpGet("watering-frequency/{plantId:guid}")]
    [ProducesResponseType(typeof(WateringFrequencyResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WateringFrequencyResponseDto>> GetWateringFrequency(
        Guid plantId,
        [FromQuery] string? period,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetWateringFrequencyQuery(plantId, period), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Counts plants added and deleted for the current user since a given date.
    /// </summary>
    /// <response code="200">Returns the plant changes.</response>
    /// <response code="400">The request was invalid.</response>
    [HttpGet("plant-changes")]
    [ProducesResponseType(typeof(PlantChangesResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PlantChangesResponseDto>> GetPlantChanges(
        [FromQuery] DateTime? since,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetPlantChangesQuery(since), cancellationToken);
        return Ok(result);
    }
}
