using EcbatanLocation.Application.Messaging;
using EcbatanLocation.Domain.Repositories;

namespace EcbatanLocation.Application.Commands.DeleteStudio;

public class DeleteStudioCommandHandler(
    IStudioRepository studioRepository,
    IReservationRepository reservationRepository) : IRequestHandler<DeleteStudioCommand>
{
    public async Task Handle(DeleteStudioCommand request, CancellationToken cancellationToken)
    {
        var studio = await studioRepository.GetByIdAsync(request.StudioId, cancellationToken)
                     ?? throw new InvalidOperationException($"Studio '{request.StudioId}' not found.");

        var hasReservations = await reservationRepository.ExistsByStudioAsync(request.StudioId, cancellationToken);
        if (hasReservations)
            throw new InvalidOperationException("Impossible de supprimer un studio qui a des réservations.");

        await studioRepository.DeleteAsync(request.StudioId, cancellationToken);
    }
}
