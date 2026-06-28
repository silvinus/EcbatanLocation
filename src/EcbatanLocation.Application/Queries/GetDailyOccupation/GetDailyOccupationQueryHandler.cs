using EcbatanLocation.Application.Messaging;
using EcbatanLocation.Application.DTOs;
using EcbatanLocation.Domain.Enums;
using EcbatanLocation.Domain.Repositories;

namespace EcbatanLocation.Application.Queries.GetDailyOccupation;

// H2: Occupied places = occupancy units of studios with at least one Accepted or Confirmed
// reservation. A whole-lodging studio counts as a single block (its full capacity); a per-bed
// studio counts the number of occupied beds out of its bed count.
public class GetDailyOccupationQueryHandler(
    IReservationRepository reservationRepository,
    IStudioRepository studioRepository) : IRequestHandler<GetDailyOccupationQuery, DailyOccupationDto>
{
    public async Task<DailyOccupationDto> Handle(GetDailyOccupationQuery request, CancellationToken cancellationToken)
    {
        var allStudios = await studioRepository.GetAllAsync(cancellationToken);
        var studios = allStudios.Where(s => !s.Unavailable).ToList();
        var reservations = await reservationRepository.GetByDateAsync(request.Date, cancellationToken);

        var activeByStudio = reservations
            .Where(r => r.Status is ReservationStatus.Accepted or ReservationStatus.Confirmed)
            .GroupBy(r => r.StudioId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var totalCapacity = studios.Sum(s => s.OccupancyCapacity);

        var occupiedPlaces = 0;
        var occupiedStudios = 0;
        foreach (var studio in studios)
        {
            if (!activeByStudio.TryGetValue(studio.Id, out var active))
                continue;

            occupiedStudios++;
            occupiedPlaces += studio.IsPerBed
                ? Math.Min(active.Sum(r => r.BedCount), studio.NumberOfBeds)
                : studio.Capacity;
        }

        return new DailyOccupationDto(
            request.Date,
            totalCapacity,
            occupiedPlaces,
            totalCapacity - occupiedPlaces,
            occupiedStudios,
            studios.Count);
    }
}
