using GardenSystem.Application.Repositories;
using GardenSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GardenSystem.Infrastructure.Persistence.Repositories;

public sealed class PlantRepository(GardenDbContext dbContext) : IPlantRepository
{
    public async Task<Plant?> GetByIdAsync(Guid plantId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Plants
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PlantId == plantId, cancellationToken);
    }

    public async Task<IReadOnlyList<Plant>> ListByGardenIdAsync(Guid gardenId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Plants
            .AsNoTracking()
            .Where(p => p.GardenId == gardenId)
            .OrderBy(p => p.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Plant>> ListPageByGardenIdAsync(
        Guid gardenId, int skip, int take, CancellationToken cancellationToken = default)
    {
        return await dbContext.Plants
            .AsNoTracking()
            .Where(p => p.GardenId == gardenId)
            .OrderBy(p => p.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Plant plant, CancellationToken cancellationToken = default)
    {
        await dbContext.Plants.AddAsync(plant, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Plant plant, CancellationToken cancellationToken = default)
    {
        dbContext.Plants.Update(plant);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SoftDeleteAsync(Guid plantId, CancellationToken cancellationToken = default)
    {
        var plant = await dbContext.Plants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.PlantId == plantId, cancellationToken);

        if (plant is null || plant.DeletedAtUtc is not null)
        {
            return;
        }

        plant.DeletedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}