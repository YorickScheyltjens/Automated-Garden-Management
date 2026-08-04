namespace GardenSystem.Application.Gardens.Dtos;

/// <summary>
/// Garden details returned by API endpoints.
/// </summary>
public sealed class GardenResponseDto
{
    /// <summary>
    /// Unique garden identifier.
    /// </summary>
    public Guid GardenId { get; init; }

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

    /// <summary>
    /// UTC timestamp when the garden was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; init; }
}
