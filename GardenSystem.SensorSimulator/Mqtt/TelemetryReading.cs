namespace GardenSystem.SensorSimulator.Mqtt;

public sealed record TelemetryReading(
    Guid PlantId,
    decimal CurrentHumidityLevel,
    bool IsCurrentlyIrrigating,
    DateTime TimestampUtc);
