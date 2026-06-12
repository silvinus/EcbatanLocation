using EcbatanLocation.Application.Messaging;
using EcbatanLocation.Application.DTOs;
using EcbatanLocation.Domain.Repositories;

namespace EcbatanLocation.Application.Queries.GetMonthlyPlanning;

public class GetMonthlyPlanningQueryHandler(
    IReservationRepository reservationRepository,
    IStudioRepository studioRepository,
    IOwnerRepository ownerRepository) : IRequestHandler<GetMonthlyPlanningQuery, MonthlyPlanningDto>
{
    public async Task<MonthlyPlanningDto> Handle(GetMonthlyPlanningQuery request, CancellationToken cancellationToken)
    {
        var studios = await studioRepository.GetAllAsync(cancellationToken);
        var owners = await ownerRepository.GetAllAsync(cancellationToken);
        var reservations = await reservationRepository.GetByMonthAsync(request.Year, request.Month, cancellationToken);

        var ownerLookup = owners.ToDictionary(o => o.Id, o => o.Name);

        var filteredReservations = reservations.AsEnumerable();

        if (request.StudioId.HasValue)
            filteredReservations = filteredReservations.Where(r => r.StudioId == request.StudioId.Value);
        if (request.Status.HasValue)
            filteredReservations = filteredReservations.Where(r => r.Status == request.Status.Value);
        if (request.OwnerId.HasValue)
            filteredReservations = filteredReservations.Where(r => r.OwnerId == request.OwnerId.Value);

        var reservationsByStudio = filteredReservations
            .GroupBy(r => r.StudioId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var filteredStudios = request.StudioId.HasValue
            ? studios.Where(s => s.Id == request.StudioId.Value)
            : studios;

        var studioPlannings = filteredStudios
            .OrderBy(s => s.DisplayOrder)
            .Select(s => new StudioPlanningDto(
                new StudioDto(s.Id, s.Name, s.Capacity, s.HasKitchen, s.RentableAlone, s.Unavailable, s.DisplayOrder),
                reservationsByStudio.TryGetValue(s.Id, out var studioReservations)
                    ? studioReservations.Select(r => new ReservationPlanningDto(
                        r.Id,
                        r.TenantName,
                        ownerLookup.GetValueOrDefault(r.OwnerId, "Unknown"),
                        r.OwnerId,
                        r.Dates.StartDate,
                        r.Dates.EndDate,
                        r.Status,
                        r.PersonLines.Select(pl => new PersonLineDto(pl.ClientType, pl.AdultCount, pl.ChildrenUnder3Count)).ToList())).ToList()
                    : []))
            .ToList();

        return new MonthlyPlanningDto(request.Year, request.Month, studioPlannings);
    }
}
