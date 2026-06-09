using MediatR;
using PlanningLocation.Domain.Repositories;
using PlanningLocation.Domain.Services;
using PlanningLocation.Domain.ValueObjects;

namespace PlanningLocation.Application.Commands.UpdateReservation;

public class UpdateReservationCommandHandler(
    IReservationRepository reservationRepository,
    IStudioRepository studioRepository,
    ReservationDomainService domainService) : IRequestHandler<UpdateReservationCommand>
{
    public async Task Handle(UpdateReservationCommand request, CancellationToken cancellationToken)
    {
        var reservation = await reservationRepository.GetByIdAsync(request.ReservationId, cancellationToken)
                          ?? throw new InvalidOperationException($"Reservation '{request.ReservationId}' not found.");

        var studio = await studioRepository.GetByIdAsync(request.StudioId, cancellationToken)
                     ?? throw new InvalidOperationException($"Studio '{request.StudioId}' not found.");

        var dates = new DateRange(request.StartDate, request.EndDate);

        var overlapExists = await reservationRepository.ExistsOverlapAsync(
            request.StudioId, dates, request.ReservationId, cancellationToken);
        domainService.ValidateNoOverlap(overlapExists);

        if (!studio.RentableAlone)
        {
            var ownerReservations = await reservationRepository.GetByOwnerAndOverlappingDatesAsync(
                reservation.OwnerId, dates, cancellationToken);
            domainService.ValidateStudioDependency(studio, reservation.OwnerId, dates, ownerReservations);
        }

        reservation.Update(
            dates,
            request.TenantName,
            request.AdultCount,
            request.ChildrenUnder3Count,
            request.ClientType,
            studio.Capacity);

        await reservationRepository.UpdateAsync(reservation, cancellationToken);
    }
}
