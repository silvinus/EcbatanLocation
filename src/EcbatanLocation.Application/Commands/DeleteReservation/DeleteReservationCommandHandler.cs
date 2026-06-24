using EcbatanLocation.Application.Messaging;
using EcbatanLocation.Domain.Repositories;

namespace EcbatanLocation.Application.Commands.DeleteReservation;

public class DeleteReservationCommandHandler(
    IReservationRepository reservationRepository) : IRequestHandler<DeleteReservationCommand>
{
    public async Task Handle(DeleteReservationCommand request, CancellationToken cancellationToken)
    {
        var reservation = await reservationRepository.GetByIdAsync(request.ReservationId, cancellationToken)
                          ?? throw new InvalidOperationException($"Reservation '{request.ReservationId}' not found.");

        reservation.MarkDeleted();
        await reservationRepository.UpdateAsync(reservation, cancellationToken);
    }
}
