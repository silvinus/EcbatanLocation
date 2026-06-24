using EcbatanLocation.Application.Messaging;
using EcbatanLocation.Domain.Repositories;
using EcbatanLocation.Domain.Services;

namespace EcbatanLocation.Application.Commands.AcceptReservation;

public class AcceptReservationCommandHandler(
    IReservationRepository reservationRepository,
    ReservationDomainService domainService) : IRequestHandler<AcceptReservationCommand>
{
    public async Task Handle(AcceptReservationCommand request, CancellationToken cancellationToken)
    {
        var reservation = await reservationRepository.GetByIdAsync(request.ReservationId, cancellationToken)
                          ?? throw new InvalidOperationException($"Reservation '{request.ReservationId}' not found.");

        if (reservation.HasParent)
            throw new InvalidOperationException(
                "Dependent reservations cannot be accepted independently. Accept the parent reservation instead.");

        reservation.Accept(request.AcceptedBy);
        await reservationRepository.UpdateAsync(reservation, cancellationToken);

        var dependents = await reservationRepository.GetDependentsByParentIdAsync(reservation.Id, cancellationToken);
        if (dependents.Count > 0)
        {
            domainService.PropagateStatusToDependents(reservation, dependents);
            await reservationRepository.UpdateRangeAsync(dependents, cancellationToken);
        }
    }
}
