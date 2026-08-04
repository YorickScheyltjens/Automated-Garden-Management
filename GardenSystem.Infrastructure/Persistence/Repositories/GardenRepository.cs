using GardenSystem.Application.Repositories;
using GardenSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GardenSystem.Infrastructure.Persistence.Repositories;

public sealed class GardenRepository(GardenDbContext dbContext) : IGardenRepository
{
    public async Task<Garden?> GetByIdAsync(Guid gardenId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Gardens
            .FirstOrDefaultAsync(g => g.GardenId == gardenId, cancellationToken);
    }

    public async Task<IReadOnlyList<Garden>> ListByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Gardens
            .Where(g => g.UserId == userId)
            .OrderBy(g => g.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Garden garden, CancellationToken cancellationToken = default)
    {
        await dbContext.Gardens.AddAsync(garden, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Garden garden, CancellationToken cancellationToken = default)
    {
        dbContext.Gardens.Update(garden);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SoftDeleteAsync(Guid gardenId, CancellationToken cancellationToken = default)
    {
        var garden = await dbContext.Gardens
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(g => g.GardenId == gardenId, cancellationToken);

        if (garden is null || garden.DeletedAtUtc is not null)
        {
            return;
        }

        garden.DeletedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}