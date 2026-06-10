using PlanningLocation.Application.Messaging;
using PlanningLocation.Application.DTOs;
using PlanningLocation.Domain.Enums;
using PlanningLocation.Domain.Repositories;

namespace PlanningLocation.Application.Queries.GetDailyOccupation;

// H2: Occupied places = max capacity of studios with at least one Accepted or Confirmed reservation.
// A studio is counted as a whole (free or occupied).
public class GetDailyOccupationQueryHandler(
    IReservationRepository reservationRepository,
    IStudioRepository studioRepository) : IRequestHandler<GetDailyOccupationQuery, DailyOccupationDto>
{
    public async Task<DailyOccupationDto> Handle(GetDailyOccupationQuery request, CancellationToken cancellationToken)
    {
        var studios = await studioRepository.GetAllAsync(cancellationToken);
        var reservations = await reservationRepository.GetByDateAsync(request.Date, cancellationToken);

        var occupiedStudioIds = reservations
            .Where(r => r.Status is ReservationStatus.Accepted or ReservationStatus.Confirmed)
            .Select(r => r.StudioId)
            .Distinct()
            .ToHashSet();

        var totalCapacity = studios.Sum(s => s.Capacity);
        var occupiedPlaces = studios.Where(s => occupiedStudioIds.Contains(s.Id)).Sum(s => s.Capacity);

        return new DailyOccupationDto(
            request.Date,
            totalCapacity,
            occupiedPlaces,
            totalCapacity - occupiedPlaces,
            occupiedStudioIds.Count,
            studios.Count);
    }
}
