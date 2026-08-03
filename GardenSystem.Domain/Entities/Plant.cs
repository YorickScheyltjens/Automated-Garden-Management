using GardenSystem.Domain.Enums;

namespace GardenSystem.Domain.Entities;

public sealed class Plant
{
    public Guid PlantId { get; set; }
    public Guid GardenId { get; set; }
    public string PlantName { get; set; } = string.Empty;
    public string Species { get; set; } = string.Empty;
    public PlantType PlantType { get; set; }
    public DateOnly PlantationDate { get; set; }
    public decimal SurfaceAreaRequired { get; set; }
    public int IdealHumidityLevel { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
}