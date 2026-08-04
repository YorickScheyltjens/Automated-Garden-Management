namespace GardenSystem.TelemetryConsumer.Messaging;

public sealed record TelemetryMessage(
    Guid PlantId,
    decimal CurrentHumidityLevel,
    bool IsCurrentlyIrrigating,
    DateTime TimestampUtc);
