namespace GardenSystem.Domain.Enums;

public static class IrrigationRates
{
    private static readonly Dictionary<PlantType, (decimal DecayRate, decimal RecoveryRate)> Rates = new()
    {
        [PlantType.Vegetable] = (DecayRate: 1m, RecoveryRate: 16m),
        [PlantType.Fruit] = (DecayRate: 3m, RecoveryRate: 18m),
        [PlantType.Flower] = (DecayRate: 4m, RecoveryRate: 20m)
    };

    public static decimal GetDecayRate(PlantType plantType) => GetRates(plantType).DecayRate;

    public static decimal GetRecoveryRate(PlantType plantType) => GetRates(plantType).RecoveryRate;

    private static (decimal DecayRate, decimal RecoveryRate) GetRates(PlantType plantType)
    {
        if (!Rates.TryGetValue(plantType, out var rates))
        {
            throw new ArgumentOutOfRangeException(nameof(plantType), plantType, "Unsupported plant type.");
        }

        return rates;
    }
}
