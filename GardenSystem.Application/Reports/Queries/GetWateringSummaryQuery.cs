using GardenSystem.Application.Reports.Dtos;
using MediatR;

namespace GardenSystem.Application.Reports.Queries;

public sealed record GetWateringSummaryQuery(DateTime? From, DateTime? To) : IRequest<WateringSummaryResponseDto>;
