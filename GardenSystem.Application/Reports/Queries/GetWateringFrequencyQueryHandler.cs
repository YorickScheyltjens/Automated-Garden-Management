using GardenSystem.Application.Abstractions;
using GardenSystem.Application.Reports.Dtos;
using GardenSystem.Application.Repositories;
using GardenSystem.Domain.Exceptions;
using MediatR;

namespace GardenSystem.Application.Reports.Queries;

public sealed class GetWateringFrequencyQueryHandler(
    IPlantRepository plantRepository,
    IGardenRepository gardenRepository,
    IReportingRepository reportingRepository,
    ICurrentUserProvider currentUserProvider) : IRequestHandler<GetWateringFrequencyQuery, WateringFrequencyResponseDto>
{
    public async Task<WateringFrequencyResponseDto> Handle(GetWateringFrequencyQuery request, CancellationToken cancellationToken)
    {
        var plant = await plantRepository.GetByIdAsync(request.PlantId, cancellationToken);
        if (plant is null)
        {
            throw new NotFoundException($"Plant with id '{request.PlantId}' was not found.");
        }

        var garden = await gardenRepository.GetByIdAsync(plant.GardenId, cancellationToken);
        if (garden is null || garden.UserId != currentUserProvider.GetCurrentUserId())
        {
            throw new NotFoundException($"Plant with id '{request.PlantId}' was not found.");
        }

        var periodStart = DateTime.UtcNow - ParsePeriod(request.Period!);

        var events = await reportingRepository.GetIrrigationEventsSinceAsync(request.PlantId, periodStart, cancellationToken);

        return new WateringFrequencyResponseDto
        {
            PlantId = request.PlantId,
            EventCount = events.Count,
            Events = events.Select(irrigationEvent => irrigationEvent.ToResponseDto()).ToList()
        };
    }

    private static TimeSpan ParsePeriod(string period)
    {
        var unit = period[^1];
        var amount = int.Parse(period[..^1]);

        return unit switch
        {
            'm' => TimeSpan.FromMinutes(amount),
            'h' => TimeSpan.FromHours(amount),
            _ => throw new ArgumentOutOfRangeException(nameof(period), period, "Unsupported period unit.")
        };
    }
}
