using GardenSystem.Application.Abstractions;
using GardenSystem.Application.Repositories;
using GardenSystem.Domain.Exceptions;
using MediatR;

namespace GardenSystem.Application.Gardens.Commands;

public sealed class UpdateGardenCommandHandler(
    IGardenRepository gardenRepository,
    ICurrentUserProvider currentUserProvider) : IRequestHandler<UpdateGardenCommand, Dtos.GardenResponseDto>
{
    public async Task<Dtos.GardenResponseDto> Handle(UpdateGardenCommand request, CancellationToken cancellationToken)
    {
        var garden = await gardenRepository.GetByIdAsync(request.GardenId, cancellationToken);

        if (garden is null || garden.UserId != currentUserProvider.GetCurrentUserId())
        {
            throw new NotFoundException($"Garden with id '{request.GardenId}' was not found.");
        }

        garden.GardenName = request.GardenName;
        garden.TotalSurfaceArea = request.TotalSurfaceArea;
        garden.LocationDescription = request.LocationDescription;
        garden.Latitude = request.Latitude;
        garden.Longitude = request.Longitude;
        garden.TargetHumidityLevel = request.TargetHumidityLevel;

        await gardenRepository.UpdateAsync(garden, cancellationToken);

        return garden.ToResponseDto();
    }
}
