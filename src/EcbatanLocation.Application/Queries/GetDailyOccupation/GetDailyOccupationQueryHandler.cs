using EcbatanLocation.Application.Messaging;
using EcbatanLocation.Application.DTOs;
using EcbatanLocation.Domain.Enums;
using EcbatanLocation.Domain.Repositories;

namespace EcbatanLocation.Application.Queries.GetDailyOccupation;

// H2: Occupied places = max capacity of studios with at least one Accepted or Confirmed reservation.
// A studio is counted as a whole (free or occupied).
public class GetDailyOccupationQueryHandler(
    IReservationRepository reservationRepository,
    IStudioRepository studioRepository) : IRequestHandler<GetDailyOccupationQuery, DailyOccupationDto>
{
    public async Task<DailyOccupationDto> Handle(GetDailyOccupationQuery request, CancellationToken cancellationToken)
    {
        var allStudios = await studioRepository.GetAllAsync(cancellationToken);
        var studios = allStudios.Where(s => !s.Unavailable).ToList();
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
            occupiedStudioIds.Count(id => studios.Any(s => s.Id == id)),
            studios.Count);
    }
}
