using GardenSystem.Domain.Entities;

namespace GardenSystem.Application.Repositories;

public interface IIrrigationEventRepository
{
    Task<IrrigationEvent?> GetOpenEventByPlantIdAsync(Guid plantId, CancellationToken cancellationToken = default);
    Task AddAsync(IrrigationEvent irrigationEvent, CancellationToken cancellationToken = default);
    Task UpdateAsync(IrrigationEvent irrigationEvent, CancellationToken cancellationToken = default);
}
