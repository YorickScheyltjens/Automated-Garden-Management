using MediatR;

namespace GardenSystem.Application.Plants.Commands;

public sealed record DeletePlantCommand(Guid PlantId) : IRequest<Unit>;
