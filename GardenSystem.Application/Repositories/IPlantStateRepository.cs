using GardenSystem.Domain.Entities;

namespace GardenSystem.Application.Repositories;

public interface IPlantStateRepository
{
    Task<PlantState?> GetByPlantIdAsync(Guid plantId, CancellationToken cancellationToken = default);
    Task AddAsync(PlantState plantState, CancellationToken cancellationToken = default);
    Task UpdateAsync(PlantState plantState, CancellationToken cancellationToken = default);
}
