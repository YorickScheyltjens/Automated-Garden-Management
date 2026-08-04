using GardenSystem.Domain.Entities;

namespace GardenSystem.Application.Repositories;

public interface IGardenRepository
{
    Task<Garden?> GetByIdAsync(Guid gardenId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Garden>> ListByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(Garden garden, CancellationToken cancellationToken = default);
    Task UpdateAsync(Garden garden, CancellationToken cancellationToken = default);
    Task SoftDeleteAsync(Guid gardenId, CancellationToken cancellationToken = default);
}