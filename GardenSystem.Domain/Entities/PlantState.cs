namespace GardenSystem.Domain.Entities;

public sealed class PlantState
{
    public Guid PlantId { get; set; }
    public decimal CurrentHumidityLevel { get; set; }
    public DateTime? LastIrrigationStartTime { get; set; }
    public DateTime? LastIrrigationEndTime { get; set; }
    public bool IsCurrentlyIrrigating { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
