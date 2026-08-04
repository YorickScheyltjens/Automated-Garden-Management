using GardenSystem.Application.Abstractions;
using GardenSystem.Application.Reports.Dtos;
using GardenSystem.Application.Repositories;
using MediatR;

namespace GardenSystem.Application.Reports.Queries;

public sealed class GetPlantChangesQueryHandler(
    IReportingRepository reportingRepository,
    ICurrentUserProvider currentUserProvider) : IRequestHandler<GetPlantChangesQuery, PlantChangesResponseDto>
{
    public async Task<PlantChangesResponseDto> Handle(GetPlantChangesQuery request, CancellationToken cancellationToken)
    {
        var (added, deleted) = await reportingRepository.GetPlantChangesAsync(
            currentUserProvider.GetCurrentUserId(),
            request.Since!.Value,
            cancellationToken);

        return new PlantChangesResponseDto
        {
            Added = added,
            Deleted = deleted
        };
    }
}
