using EcbatanLocation.Application.Messaging;
using EcbatanLocation.Domain.Repositories;

namespace EcbatanLocation.Application.Commands.ConfirmReservation;

public class ConfirmReservationCommandHandler(
    IReservationRepository reservationRepository) : IRequestHandler<ConfirmReservationCommand>
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
    }
}
