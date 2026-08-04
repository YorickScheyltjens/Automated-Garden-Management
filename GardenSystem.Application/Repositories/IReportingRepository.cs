using GardenSystem.Domain.Entities;

namespace GardenSystem.Application.Repositories;

public interface IReportingRepository
{
    Task<(int WateredCount, int UnwateredCount)> GetWateringSummaryAsync(
        Guid userId, DateTime from, DateTime to, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IrrigationEvent>> GetIrrigationEventsSinceAsync(
        Guid plantId, DateTime periodStart, CancellationToken cancellationToken = default);

    Task<(int Added, int Deleted)> GetPlantChangesAsync(
        Guid userId, DateTime since, CancellationToken cancellationToken = default);
}
