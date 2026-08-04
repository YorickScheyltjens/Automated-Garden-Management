namespace GardenSystem.Domain;

public static class TelemetryEvaluator
{
    public static bool ShouldStartWatering(decimal currentHumidityLevel, int idealHumidityLevel, bool isCurrentlyIrrigating) =>
        !isCurrentlyIrrigating && currentHumidityLevel < idealHumidityLevel;
}
