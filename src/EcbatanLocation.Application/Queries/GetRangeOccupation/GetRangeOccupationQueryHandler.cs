using EcbatanLocation.Application.Messaging;
using EcbatanLocation.Application.DTOs;
using EcbatanLocation.Domain.Enums;
using EcbatanLocation.Domain.Repositories;

namespace EcbatanLocation.Application.Queries.GetRangeOccupation;

public class GetRangeOccupationQueryHandler(
    IReservationRepository reservationRepository,
    IStudioRepository studioRepository) : IRequestHandler<GetRangeOccupationQuery, RangeOccupationDto>
{
    public async Task<RangeOccupationDto> Handle(GetRangeOccupationQuery request, CancellationToken cancellationToken)
    {
        var allStudios = await studioRepository.GetAllAsync(cancellationToken);
        var studios = allStudios.Where(s => !s.Unavailable).ToList();
        var totalCapacity = studios.Sum(s => s.OccupancyCapacity);
        var totalStudios = studios.Count;

        var days = 0;
        var totalOccupiedPlaces = 0;

        for (var date = request.StartDate; date <= request.EndDate; date = date.AddDays(1))
        {
            days++;
            var reservations = await reservationRepository.GetByDateAsync(date, cancellationToken);

            var activeByStudio = reservations
                .Where(r => r.Status is ReservationStatus.Accepted or ReservationStatus.Confirmed)
                .GroupBy(r => r.StudioId)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var studio in studios)
            {
                if (!activeByStudio.TryGetValue(studio.Id, out var active))
                    continue;

                totalOccupiedPlaces += studio.IsPerBed
                    ? Math.Min(active.Sum(r => r.BedCount), studio.NumberOfBeds)
                    : studio.Capacity;
            }
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
