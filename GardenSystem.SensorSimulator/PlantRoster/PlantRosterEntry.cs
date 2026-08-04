using GardenSystem.Domain.Enums;

namespace GardenSystem.SensorSimulator.PlantRoster;

public sealed record PlantRosterEntry(Guid PlantId, PlantType PlantType);
