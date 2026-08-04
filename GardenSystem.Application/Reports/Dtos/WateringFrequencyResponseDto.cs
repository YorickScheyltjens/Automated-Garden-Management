namespace GardenSystem.Application.Reports.Dtos;

/// <summary>
/// Irrigation frequency for a single plant within a period.
/// </summary>
public sealed class WateringFrequencyResponseDto
{
    /// <summary>
    /// Identifier of the plant this report covers.
    /// </summary>
    public Guid PlantId { get; init; }

    /// <summary>
    /// Number of irrigation events within the period.
    /// </summary>
    public int EventCount { get; init; }

    /// <summary>
    /// The irrigation events within the period, most recent first.
    /// </summary>
    public IReadOnlyList<IrrigationEventResponseDto> Events { get; init; } = [];
}
