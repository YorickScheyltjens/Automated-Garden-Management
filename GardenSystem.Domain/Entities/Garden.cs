namespace GardenSystem.Domain.Entities;

public sealed class Garden
{
    public Guid GardenId { get; set; }
    public Guid UserId { get; set; }
    public string GardenName { get; set; } = string.Empty;
    public decimal TotalSurfaceArea { get; set; }
    public string LocationDescription { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public int TargetHumidityLevel { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? DeletedAtUtc { get; set; }

    public bool CanFitPlant(IEnumerable<Plant> existingPlants, decimal newPlantSurfaceArea)
    {
        var usedSurfaceArea = existingPlants
            .Where(plant => plant.DeletedAtUtc is null)
            .Sum(plant => plant.SurfaceAreaRequired);

        return usedSurfaceArea + newPlantSurfaceArea <= TotalSurfaceArea;
    }
}