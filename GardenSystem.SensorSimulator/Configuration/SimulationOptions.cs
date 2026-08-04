namespace GardenSystem.SensorSimulator.Configuration;

public sealed class SimulationOptions
{
    public int TickIntervalSeconds { get; set; } = 5;
    public int RosterPollIntervalSeconds { get; set; } = 30;
}
