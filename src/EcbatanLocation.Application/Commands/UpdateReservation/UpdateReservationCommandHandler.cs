using EcbatanLocation.Application.Messaging;
using EcbatanLocation.Domain.Repositories;
using EcbatanLocation.Domain.Services;
using EcbatanLocation.Domain.ValueObjects;

namespace EcbatanLocation.Application.Commands.UpdateReservation;

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

        if (studio.Unavailable)
            throw new InvalidOperationException($"Studio '{studio.Name}' is currently unavailable and cannot be reserved.");

        var dates = new DateRange(request.StartDate, request.EndDate);

        var overlapExists = await reservationRepository.ExistsOverlapAsync(
            request.StudioId, dates, request.ReservationId, cancellationToken);
        domainService.ValidateNoOverlap(overlapExists);

        var hasDependents = await reservationRepository.HasDependentsAsync(reservation.Id, cancellationToken);
        if (hasDependents && (dates.StartDate != reservation.Dates.StartDate || dates.EndDate != reservation.Dates.EndDate))
            throw new InvalidOperationException(
                "Cannot change dates: this reservation has dependent reservations.");

        if (!studio.RentableAlone)
        {
            if (request.ParentReservationId is null)
                throw new InvalidOperationException(
                    $"Studio '{studio.Name}' is not rentable alone. A parent reservation must be specified.");

            var parent = await reservationRepository.GetByIdAsync(request.ParentReservationId.Value, cancellationToken)
                         ?? throw new InvalidOperationException("Parent reservation not found.");

            var parentStudio = await studioRepository.GetByIdAsync(parent.StudioId, cancellationToken)
                               ?? throw new InvalidOperationException("Parent studio not found.");

            domainService.ValidateParentLink(studio, parent, parentStudio, dates, reservation.OwnerId);
            reservation.SetParentReservation(parent.Id);
        }
        else
        {
            reservation.ClearParentReservation();
        }

        var personLines = request.PersonLines
            .Select(pl => new Domain.ValueObjects.PersonLine(pl.ClientType, pl.AdultCount, pl.ChildrenUnder3Count))
            .ToList();

        reservation.Update(
            dates,
            request.TenantName,
            personLines,
            studio.Capacity);

        await reservationRepository.UpdateAsync(reservation, cancellationToken);
    }
}
