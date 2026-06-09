using MediatR;
using PlanningLocation.Application.DTOs;
using PlanningLocation.Domain.Enums;
using PlanningLocation.Domain.Repositories;

namespace PlanningLocation.Application.Queries.GetReservationDetail;

public class GetReservationDetailQueryHandler(
    IReservationRepository reservationRepository,
    IStudioRepository studioRepository,
    IOwnerRepository ownerRepository,
    IPricingGridRepository pricingGridRepository) : IRequestHandler<GetReservationDetailQuery, ReservationDetailDto?>
{
    public async Task<ReservationDetailDto?> Handle(GetReservationDetailQuery request, CancellationToken cancellationToken)
    {
        var reservation = await reservationRepository.GetByIdAsync(request.ReservationId, cancellationToken);
        if (reservation is null) return null;

        var studio = await studioRepository.GetByIdAsync(reservation.StudioId, cancellationToken);
        var owner = await ownerRepository.GetByIdAsync(reservation.OwnerId, cancellationToken);

        decimal? estimatedAmount = null;
        var pricingGrid = await pricingGridRepository.GetByYearAsync(reservation.Dates.StartDate.Year, cancellationToken);
        if (pricingGrid is not null)
        {
            var rate = pricingGrid.GetRate(reservation.ClientType);
            var days = reservation.Dates.NumberOfDays;
            var childRate = reservation.ClientType == ClientType.Acquaintance ? rate * 0.5m : rate;
            estimatedAmount = (reservation.AdultCount * rate + reservation.ChildrenUnder3Count * childRate) * days;
        }

        return new ReservationDetailDto(
            reservation.Id,
            studio is not null
                ? new StudioDto(studio.Id, studio.Name, studio.Capacity, studio.HasKitchen, studio.RentableAlone, studio.DisplayOrder)
                : new StudioDto(reservation.StudioId, "Unknown", 0, false, false, 0),
            owner is not null
                ? new OwnerDto(owner.Id, owner.Name)
                : new OwnerDto(reservation.OwnerId, "Unknown"),
            reservation.Dates.StartDate,
            reservation.Dates.EndDate,
            reservation.Dates.NumberOfDays,
            reservation.TenantName,
            reservation.AdultCount,
            reservation.ChildrenUnder3Count,
            reservation.ClientType,
            reservation.Status,
            reservation.AcceptedBy,
            reservation.AcceptedAt,
            reservation.ConfirmedBy,
            reservation.ConfirmedAt,
            reservation.CreatedAt,
            reservation.UpdatedAt,
            estimatedAmount);
    }
}
