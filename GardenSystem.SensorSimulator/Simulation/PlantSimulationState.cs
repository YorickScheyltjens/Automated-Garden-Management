using GardenSystem.Domain.Enums;

namespace GardenSystem.SensorSimulator.Simulation;

public sealed class PlantSimulationState(Guid plantId, PlantType plantType, decimal initialHumidityLevel)
{
    private const int RecoveryTicks = 2;

    private int _irrigationTicksElapsed;
    private decimal _irrigationStartHumidityLevel;

    public Guid PlantId { get; } = plantId;

    public PlantType PlantType { get; } = plantType;

    public decimal CurrentHumidityLevel { get; private set; } = initialHumidityLevel;

    public bool IsCurrentlyIrrigating { get; private set; }

    public void StartIrrigating()
    {
        if (IsCurrentlyIrrigating)
        {
            return;
        }

        IsCurrentlyIrrigating = true;
        _irrigationTicksElapsed = 0;
        _irrigationStartHumidityLevel = CurrentHumidityLevel;
    }

    public void Tick()
    {
        if (IsCurrentlyIrrigating)
        {
            _irrigationTicksElapsed++;

            var recoveryRate = IrrigationRates.GetRecoveryRate(PlantType);
            var recoveredSoFar = _irrigationTicksElapsed >= RecoveryTicks
                ? recoveryRate
                : recoveryRate / RecoveryTicks;

            CurrentHumidityLevel = Clamp(_irrigationStartHumidityLevel + recoveredSoFar);

            if (_irrigationTicksElapsed >= RecoveryTicks)
            {
                IsCurrentlyIrrigating = false;
            }
        }
        else
        {
            CurrentHumidityLevel = Clamp(CurrentHumidityLevel - IrrigationRates.GetDecayRate(PlantType));
        }
    }

    private static decimal Clamp(decimal value) => Math.Clamp(value, 0m, 100m);
}
