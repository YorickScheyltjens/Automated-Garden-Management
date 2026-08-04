using GardenSystem.Application.Abstractions;
using GardenSystem.Application.Repositories;
using GardenSystem.Domain.Entities;
using MediatR;

namespace GardenSystem.Application.Gardens.Commands;

public sealed class CreateGardenCommandHandler(
    IGardenRepository gardenRepository,
    ICurrentUserProvider currentUserProvider) : IRequestHandler<CreateGardenCommand, Dtos.GardenResponseDto>
{
    public async Task<Dtos.GardenResponseDto> Handle(CreateGardenCommand request, CancellationToken cancellationToken)
    {
        var garden = new Garden
        {
            GardenId = Guid.NewGuid(),
            UserId = currentUserProvider.GetCurrentUserId(),
            GardenName = request.GardenName,
            TotalSurfaceArea = request.TotalSurfaceArea,
            LocationDescription = request.LocationDescription,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            TargetHumidityLevel = request.TargetHumidityLevel,
            CreatedAtUtc = DateTime.UtcNow
        };

        await gardenRepository.AddAsync(garden, cancellationToken);

        return garden.ToResponseDto();
    }
}
