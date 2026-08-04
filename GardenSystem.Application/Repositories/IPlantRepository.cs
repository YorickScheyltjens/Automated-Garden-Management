using GardenSystem.Domain.Entities;

namespace GardenSystem.Application.Repositories;

public interface IPlantRepository
{
    Task<Plant?> GetByIdAsync(Guid plantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Plant>> ListByGardenIdAsync(Guid gardenId, CancellationToken cancellationToken = default);
    Task AddAsync(Plant plant, CancellationToken cancellationToken = default);
    Task UpdateAsync(Plant plant, CancellationToken cancellationToken = default);
    Task SoftDeleteAsync(Guid plantId, CancellationToken cancellationToken = default);
}