using GardenSystem.Application.Abstractions;
using GardenSystem.Application.Reports.Dtos;
using GardenSystem.Application.Repositories;
using MediatR;

namespace GardenSystem.Application.Reports.Queries;

public sealed class GetWateringSummaryQueryHandler(
    IReportingRepository reportingRepository,
    ICurrentUserProvider currentUserProvider) : IRequestHandler<GetWateringSummaryQuery, WateringSummaryResponseDto>
{
    public async Task<WateringSummaryResponseDto> Handle(GetWateringSummaryQuery request, CancellationToken cancellationToken)
    {
        var (wateredCount, unwateredCount) = await reportingRepository.GetWateringSummaryAsync(
            currentUserProvider.GetCurrentUserId(),
            request.From!.Value,
            request.To!.Value,
            cancellationToken);

        return new WateringSummaryResponseDto
        {
            WateredCount = wateredCount,
            UnwateredCount = unwateredCount
        };
    }
}
