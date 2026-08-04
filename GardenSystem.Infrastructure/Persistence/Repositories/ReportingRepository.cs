using GardenSystem.Application.Repositories;
using GardenSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GardenSystem.Infrastructure.Persistence.Repositories;

public sealed class ReportingRepository(GardenDbContext dbContext) : IReportingRepository
{
    public async Task<(int WateredCount, int UnwateredCount)> GetWateringSummaryAsync(
        Guid userId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        from = EnsureUtc(from);
        to = EnsureUtc(to);

        var plantIdsQuery = dbContext.Plants
            .Join(dbContext.Gardens, plant => plant.GardenId, garden => garden.GardenId, (plant, garden) => new { plant.PlantId, garden.UserId })
            .Where(joined => joined.UserId == userId)
            .Select(joined => joined.PlantId);

        var totalPlantCount = await plantIdsQuery.CountAsync(cancellationToken);

        var wateredCount = await plantIdsQuery
            .Where(plantId =>
                dbContext.PlantStates.Any(state => state.PlantId == plantId && state.IsCurrentlyIrrigating)
                || dbContext.IrrigationEvents.Any(irrigationEvent =>
                    irrigationEvent.PlantId == plantId
                    && irrigationEvent.EndTimeUtc != null
                    && irrigationEvent.EndTimeUtc >= from
                    && irrigationEvent.EndTimeUtc <= to))
            .Distinct()
            .CountAsync(cancellationToken);

        return (wateredCount, totalPlantCount - wateredCount);
    }

    public async Task<IReadOnlyList<IrrigationEvent>> GetIrrigationEventsSinceAsync(
        Guid plantId, DateTime periodStart, CancellationToken cancellationToken = default)
    {
        periodStart = EnsureUtc(periodStart);

        return await dbContext.IrrigationEvents
            .Where(irrigationEvent => irrigationEvent.PlantId == plantId && irrigationEvent.StartTimeUtc >= periodStart)
            .OrderByDescending(irrigationEvent => irrigationEvent.StartTimeUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<(int Added, int Deleted)> GetPlantChangesAsync(
        Guid userId, DateTime since, CancellationToken cancellationToken = default)
    {
        since = EnsureUtc(since);

        var gardenIdsQuery = dbContext.Gardens
            .IgnoreQueryFilters()
            .Where(garden => garden.UserId == userId)
            .Select(garden => garden.GardenId);

        var added = await dbContext.Plants
            .IgnoreQueryFilters()
            .Where(plant => gardenIdsQuery.Contains(plant.GardenId) && plant.CreatedAtUtc >= since)
            .CountAsync(cancellationToken);

        var deleted = await dbContext.Plants
            .IgnoreQueryFilters()
            .Where(plant => gardenIdsQuery.Contains(plant.GardenId) && plant.DeletedAtUtc != null && plant.DeletedAtUtc >= since)
            .CountAsync(cancellationToken);

        return (added, deleted);
    }

    private static DateTime EnsureUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
