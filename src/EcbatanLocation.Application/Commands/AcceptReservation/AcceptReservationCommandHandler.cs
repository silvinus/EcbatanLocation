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

        reservation.Accept(request.AcceptedBy);

        await reservationRepository.UpdateAsync(reservation, cancellationToken);
    }
}
