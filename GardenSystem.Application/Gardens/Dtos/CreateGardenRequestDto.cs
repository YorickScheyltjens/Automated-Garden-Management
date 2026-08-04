namespace GardenSystem.Application.Gardens.Dtos;

/// <summary>
/// Request payload to create a garden.
/// </summary>
public sealed class CreateGardenRequestDto
{
    /// <summary>
    /// Display name of the garden.
    /// </summary>
    public string GardenName { get; init; } = string.Empty;

    /// <summary>
    /// Total available surface area in square meters.
    /// </summary>
    public decimal TotalSurfaceArea { get; init; }

    /// <summary>
    /// Human-readable description of the location.
    /// </summary>
    public string LocationDescription { get; init; } = string.Empty;

    /// <summary>
    /// Latitude coordinate of the garden location.
    /// </summary>
    public decimal? Latitude { get; init; }

    /// <summary>
    /// Longitude coordinate of the garden location.
    /// </summary>
    public decimal? Longitude { get; init; }

    /// <summary>
    /// Desired humidity level for the garden in percent (0-100).
    /// </summary>
    public int TargetHumidityLevel { get; init; }
}
