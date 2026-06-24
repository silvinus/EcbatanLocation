using EcbatanLocation.Application.Messaging;
using EcbatanLocation.Domain.Repositories;
using EcbatanLocation.Domain.Services;

namespace EcbatanLocation.Application.Commands.ConfirmReservation;

public class ConfirmReservationCommandHandler(
    IReservationRepository reservationRepository,
    ReservationDomainService domainService) : IRequestHandler<ConfirmReservationCommand>
{
    public async Task Handle(ConfirmReservationCommand request, CancellationToken cancellationToken)
    {
        var reservation = await reservationRepository.GetByIdAsync(request.ReservationId, cancellationToken)
                          ?? throw new InvalidOperationException($"Reservation '{request.ReservationId}' not found.");

        if (reservation.HasParent)
            throw new InvalidOperationException(
                "Dependent reservations cannot be confirmed independently. Confirm the parent reservation instead.");

        reservation.Confirm(request.ConfirmedBy);
        await reservationRepository.UpdateAsync(reservation, cancellationToken);

        var dependents = await reservationRepository.GetDependentsByParentIdAsync(reservation.Id, cancellationToken);
        if (dependents.Count > 0)
        {
            domainService.PropagateStatusToDependents(reservation, dependents);
            await reservationRepository.UpdateRangeAsync(dependents, cancellationToken);
        }
    }
}
