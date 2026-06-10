using PlanningLocation.Application.Messaging;
using PlanningLocation.Domain.Repositories;

namespace PlanningLocation.Application.Commands.ConfirmReservation;

public class ConfirmReservationCommandHandler(
    IReservationRepository reservationRepository) : IRequestHandler<ConfirmReservationCommand>
{
    public async Task Handle(ConfirmReservationCommand request, CancellationToken cancellationToken)
    {
        var reservation = await reservationRepository.GetByIdAsync(request.ReservationId, cancellationToken)
                          ?? throw new InvalidOperationException($"Reservation '{request.ReservationId}' not found.");

        reservation.Confirm(request.ConfirmedBy);

        await reservationRepository.UpdateAsync(reservation, cancellationToken);
    }
}
