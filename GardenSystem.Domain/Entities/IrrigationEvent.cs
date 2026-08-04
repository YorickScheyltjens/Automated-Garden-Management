namespace GardenSystem.Domain.Entities;

public sealed class IrrigationEvent
{
    public Guid IrrigationEventId { get; set; }
    public Guid PlantId { get; set; }
    public DateTime StartTimeUtc { get; set; }
    public DateTime? EndTimeUtc { get; set; }
    public decimal HumidityBefore { get; set; }
    public decimal? HumidityAfter { get; set; }
}
