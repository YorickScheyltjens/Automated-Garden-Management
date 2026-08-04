namespace GardenSystem.SensorSimulator.PlantRoster;

public interface IPlantRosterClient
{
    Task<IReadOnlyList<PlantRosterEntry>> GetPlantRosterAsync(CancellationToken cancellationToken);
}
