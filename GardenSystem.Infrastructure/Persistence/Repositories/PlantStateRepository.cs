using GardenSystem.Application.Repositories;
using GardenSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GardenSystem.Infrastructure.Persistence.Repositories;

public sealed class PlantStateRepository(GardenDbContext dbContext) : IPlantStateRepository
{
    public async Task<PlantState?> GetByPlantIdAsync(Guid plantId, CancellationToken cancellationToken = default)
    {
        return await dbContext.PlantStates
            .FirstOrDefaultAsync(p => p.PlantId == plantId, cancellationToken);
    }

    public async Task AddAsync(PlantState plantState, CancellationToken cancellationToken = default)
    {
        await dbContext.PlantStates.AddAsync(plantState, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(PlantState plantState, CancellationToken cancellationToken = default)
    {
        dbContext.PlantStates.Update(plantState);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
