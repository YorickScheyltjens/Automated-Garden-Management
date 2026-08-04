using GardenSystem.Application.Abstractions;
using GardenSystem.Application.Repositories;
using GardenSystem.Domain.Exceptions;
using MediatR;

namespace GardenSystem.Application.Gardens.Queries;

public sealed class GetGardenByIdQueryHandler(
    IGardenRepository gardenRepository,
    ICurrentUserProvider currentUserProvider) : IRequestHandler<GetGardenByIdQuery, Dtos.GardenResponseDto>
{
    public async Task<Dtos.GardenResponseDto> Handle(GetGardenByIdQuery request, CancellationToken cancellationToken)
    {
        var garden = await gardenRepository.GetByIdAsync(request.GardenId, cancellationToken);

        if (garden is null || garden.UserId != currentUserProvider.GetCurrentUserId())
        {
            throw new NotFoundException($"Garden with id '{request.GardenId}' was not found.");
        }

        return garden.ToResponseDto();
    }
}
