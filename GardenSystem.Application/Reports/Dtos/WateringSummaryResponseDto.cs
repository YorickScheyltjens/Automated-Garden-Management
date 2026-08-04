namespace GardenSystem.Application.Reports.Dtos;

/// <summary>
/// Count of watered vs. unwatered plants for the current user within a period.
/// </summary>
public sealed class WateringSummaryResponseDto
{
    /// <summary>
    /// Number of plants currently irrigating, or confirmed watered within the period.
    /// </summary>
    public int WateredCount { get; init; }

    /// <summary>
    /// Number of plants not watered within the period.
    /// </summary>
    public int UnwateredCount { get; init; }
}
