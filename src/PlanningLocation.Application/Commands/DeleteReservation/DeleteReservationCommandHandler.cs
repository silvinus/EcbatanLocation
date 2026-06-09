using MediatR;
using PlanningLocation.Domain.Repositories;

namespace PlanningLocation.Application.Commands.DeleteReservation;

public class DeleteReservationCommandHandler(
    IReservationRepository reservationRepository) : IRequestHandler<DeleteReservationCommand>
{
    public async Task Handle(DeleteReservationCommand request, CancellationToken cancellationToken)
    {
        var reservation = await reservationRepository.GetByIdAsync(request.ReservationId, cancellationToken)
                          ?? throw new InvalidOperationException($"Reservation '{request.ReservationId}' not found.");

        await reservationRepository.DeleteAsync(reservation.Id, cancellationToken);
    }
}
