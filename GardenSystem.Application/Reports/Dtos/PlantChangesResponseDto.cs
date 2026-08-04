namespace GardenSystem.Application.Reports.Dtos;

/// <summary>
/// Count of plants added and deleted since a given date.
/// </summary>
public sealed class PlantChangesResponseDto
{
    /// <summary>
    /// Number of plants created since the given date.
    /// </summary>
    public int Added { get; init; }

    /// <summary>
    /// Number of plants soft-deleted since the given date.
    /// </summary>
    public int Deleted { get; init; }
}
