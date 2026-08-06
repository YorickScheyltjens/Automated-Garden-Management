using GardenSystem.Application.Abstractions;
using GardenSystem.Application.Repositories;
using MediatR;

namespace GardenSystem.Application.Gardens.Queries;

public sealed class ListGardensByUserIdQueryHandler(
    IGardenRepository gardenRepository,
    ICurrentUserProvider currentUserProvider) : IRequestHandler<ListGardensByUserIdQuery, IReadOnlyList<Dtos.GardenResponseDto>>
{
    public async Task<IReadOnlyList<Dtos.GardenResponseDto>> Handle(ListGardensByUserIdQuery request, CancellationToken cancellationToken)
    {
        var gardens = await gardenRepository.ListPageByUserIdAsync(
            currentUserProvider.GetCurrentUserId(), request.Skip, request.Take, cancellationToken);

        return gardens
            .Select(garden => garden.ToResponseDto())
            .ToList();
    }
}
