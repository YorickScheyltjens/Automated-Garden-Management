using GardenSystem.Domain.Enums;

namespace GardenSystem.Application.Plants.Dtos;

/// <summary>
/// Request payload to update a plant.
/// </summary>
public sealed class UpdatePlantRequestDto
{
    /// <summary>
    /// Identifier of the garden that contains the plant.
    /// </summary>
    public Guid GardenId { get; init; }

    /// <summary>
    /// Display name of the plant.
    /// </summary>
    public string PlantName { get; init; } = string.Empty;

    /// <summary>
    /// Botanical species of the plant.
    /// </summary>
    public string Species { get; init; } = string.Empty;

    /// <summary>
    /// Type category of the plant.
    /// </summary>
    public PlantType PlantType { get; init; }

    /// <summary>
    /// Calendar date when the plant was planted.
    /// </summary>
    public DateOnly PlantationDate { get; init; }

    /// <summary>
    /// Surface area required by the plant in square meters.
    /// </summary>
    public decimal SurfaceAreaRequired { get; init; }

    /// <summary>
    /// Desired humidity level for the plant in percent (0-100).
    /// </summary>
    public int IdealHumidityLevel { get; init; }
}
