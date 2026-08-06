using GardenSystem.Application.Repositories;
using GardenSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GardenSystem.Infrastructure.Persistence.Repositories;

public sealed class IrrigationEventRepository(GardenDbContext dbContext) : IIrrigationEventRepository
{
    public async Task<IrrigationEvent?> GetOpenEventByPlantIdAsync(Guid plantId, CancellationToken cancellationToken = default)
    {
        return await dbContext.IrrigationEvents
            .AsNoTracking()
            .Where(e => e.PlantId == plantId && e.EndTimeUtc == null)
            .OrderByDescending(e => e.StartTimeUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(IrrigationEvent irrigationEvent, CancellationToken cancellationToken = default)
    {
        await dbContext.IrrigationEvents.AddAsync(irrigationEvent, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(IrrigationEvent irrigationEvent, CancellationToken cancellationToken = default)
    {
        dbContext.IrrigationEvents.Update(irrigationEvent);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
