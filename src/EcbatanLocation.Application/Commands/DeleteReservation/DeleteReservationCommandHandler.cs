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

        var dependents = await reservationRepository.GetDependentsByParentIdAsync(reservation.Id, cancellationToken);
        if (dependents.Count > 0)
        {
            foreach (var dep in dependents)
                dep.MarkDeleted();

            await reservationRepository.DeleteRangeAsync(dependents.Select(d => d.Id), cancellationToken);
        }

        reservation.MarkDeleted();
        await reservationRepository.DeleteAsync(reservation.Id, cancellationToken);
    }
}
