using EcbatanLocation.Application.Messaging;
using EcbatanLocation.Application.Services;
using EcbatanLocation.Domain.Repositories;

namespace EcbatanLocation.Application.Commands.DeleteReservation;

public class DeleteReservationCommandHandler(
    IReservationRepository reservationRepository,
    HypotheticalPromotionService promotionService) : IRequestHandler<DeleteReservationCommand>
{
    public async Task Handle(DeleteReservationCommand request, CancellationToken cancellationToken)
    {
        var reservation = await reservationRepository.GetByIdAsync(request.ReservationId, cancellationToken)
                          ?? throw new InvalidOperationException($"Reservation '{request.ReservationId}' not found.");

        // Deleting a hypothetical frees nothing (it never occupied a slot), so no promotion follows.
        var wasReal = !reservation.IsHypothetical;
        var studioId = reservation.StudioId;
        var freedRange = reservation.Dates;

        reservation.MarkDeleted();
        await reservationRepository.UpdateAsync(reservation, cancellationToken);

        if (wasReal)
            await promotionService.PromoteFittingHypotheticalsAsync(studioId, freedRange, cancellationToken);
    }
}
