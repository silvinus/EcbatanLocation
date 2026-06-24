using EcbatanLocation.Application.Messaging;
using EcbatanLocation.Domain.Repositories;

namespace EcbatanLocation.Application.Commands.AcceptReservation;

public class AcceptReservationCommandHandler(
    IReservationRepository reservationRepository) : IRequestHandler<AcceptReservationCommand>
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
    }
}
