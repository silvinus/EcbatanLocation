using EcbatanLocation.Application.Messaging;
using EcbatanLocation.Application.DTOs;
using EcbatanLocation.Domain.Enums;
using EcbatanLocation.Domain.Repositories;

namespace EcbatanLocation.Application.Queries.GetMonthlyOccupation;

// H2: per day, occupied places = max capacity of studios with at least one Accepted
// or Confirmed reservation that day. A studio is counted as a whole (free or occupied).
// Same rule as GetDailyOccupationQueryHandler, evaluated for every day of the month.
public class GetMonthlyOccupationQueryHandler(
    IReservationRepository reservationRepository,
    IStudioRepository studioRepository) : IRequestHandler<GetMonthlyOccupationQuery, MonthlyOccupationDto>
{
    public async Task<MonthlyOccupationDto> Handle(GetMonthlyOccupationQuery request, CancellationToken cancellationToken)
    {
        var allStudios = await studioRepository.GetAllAsync(cancellationToken);
        var studios = allStudios.Where(s => !s.Unavailable).ToList();
        var reservations = await reservationRepository.GetByMonthAsync(request.Year, request.Month, cancellationToken);

        var bookingStatuses = reservations
            .Where(r => r.Status is ReservationStatus.Accepted or ReservationStatus.Confirmed)
            .ToList();

        var totalCapacity = studios.Sum(s => s.Capacity);
        var daysInMonth = DateTime.DaysInMonth(request.Year, request.Month);

        var days = new List<DailyOccupationDto>(daysInMonth);
        for (var day = 1; day <= daysInMonth; day++)
        {
            var date = new DateOnly(request.Year, request.Month, day);

            var occupiedStudioIds = bookingStatuses
                .Where(r => r.Dates.ContainsDay(date))
                .Select(r => r.StudioId)
                .Distinct()
                .ToHashSet();

            var occupiedStudios = studios.Where(s => occupiedStudioIds.Contains(s.Id)).ToList();
            var occupiedPlaces = occupiedStudios.Sum(s => s.Capacity);

            days.Add(new DailyOccupationDto(
                date,
                totalCapacity,
                occupiedPlaces,
                totalCapacity - occupiedPlaces,
                occupiedStudios.Count,
                studios.Count));
        }

        return new MonthlyOccupationDto(request.Year, request.Month, days);
    }
}
