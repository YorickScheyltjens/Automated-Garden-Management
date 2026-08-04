namespace GardenSystem.Application.Reports.Dtos;

/// <summary>
/// A single irrigation event for a plant.
/// </summary>
public sealed class IrrigationEventResponseDto
{
    /// <summary>
    /// Unique identifier of the irrigation event.
    /// </summary>
    public Guid IrrigationEventId { get; init; }

    /// <summary>
    /// UTC timestamp when the irrigation command was sent.
    /// </summary>
    public DateTime StartTimeUtc { get; init; }

    /// <summary>
    /// UTC timestamp when telemetry confirmed the recovery completed, if confirmed yet.
    /// </summary>
    public DateTime? EndTimeUtc { get; init; }

    /// <summary>
    /// Humidity level in percent when the event started.
    /// </summary>
    public decimal HumidityBefore { get; init; }

    /// <summary>
    /// Humidity level in percent once the event was confirmed complete, if confirmed yet.
    /// </summary>
    public decimal? HumidityAfter { get; init; }
}
