using PlanningLocation.Application.Messaging;
using PlanningLocation.Application.DTOs;
using PlanningLocation.Domain.Enums;
using PlanningLocation.Domain.Repositories;

namespace PlanningLocation.Application.Queries.GetRangeOccupation;

public class GetRangeOccupationQueryHandler(
    IReservationRepository reservationRepository,
    IStudioRepository studioRepository) : IRequestHandler<GetRangeOccupationQuery, RangeOccupationDto>
{
    public async Task<RangeOccupationDto> Handle(GetRangeOccupationQuery request, CancellationToken cancellationToken)
    {
        var studios = await studioRepository.GetAllAsync(cancellationToken);
        var totalCapacity = studios.Sum(s => s.Capacity);
        var totalStudios = studios.Count;

        var days = 0;
        var totalOccupiedPlaces = 0;

        for (var date = request.StartDate; date <= request.EndDate; date = date.AddDays(1))
        {
            days++;
            var reservations = await reservationRepository.GetByDateAsync(date, cancellationToken);

            var occupiedStudioIds = reservations
                .Where(r => r.Status is ReservationStatus.Accepted or ReservationStatus.Confirmed)
                .Select(r => r.StudioId)
                .Distinct()
                .ToHashSet();

            totalOccupiedPlaces += studios.Where(s => occupiedStudioIds.Contains(s.Id)).Sum(s => s.Capacity);
        }

        var avgOccupied = days > 0 ? (double)totalOccupiedPlaces / days : 0;
        var avgRate = totalCapacity > 0 && days > 0
            ? (double)totalOccupiedPlaces / (days * totalCapacity) * 100
            : 0;

        return new RangeOccupationDto(
            request.StartDate,
            request.EndDate,
            totalCapacity,
            totalStudios,
            Math.Round(avgOccupied, 1),
            Math.Round(avgRate, 1),
            days);
    }
}
