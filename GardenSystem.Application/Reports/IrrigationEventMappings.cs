using GardenSystem.Application.Reports.Dtos;
using GardenSystem.Domain.Entities;

namespace GardenSystem.Application.Reports;

internal static class IrrigationEventMappings
{
    public static IrrigationEventResponseDto ToResponseDto(this IrrigationEvent irrigationEvent)
    {
        return new IrrigationEventResponseDto
        {
            IrrigationEventId = irrigationEvent.IrrigationEventId,
            StartTimeUtc = irrigationEvent.StartTimeUtc,
            EndTimeUtc = irrigationEvent.EndTimeUtc,
            HumidityBefore = irrigationEvent.HumidityBefore,
            HumidityAfter = irrigationEvent.HumidityAfter
        };
    }
}
