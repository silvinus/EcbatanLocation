using EcbatanLocation.Application.Messaging;
using EcbatanLocation.Domain.Enums;
using EcbatanLocation.Domain.Repositories;

namespace EcbatanLocation.Application.Commands.UpdateStudio;

public class UpdateStudioCommandHandler(
    IStudioRepository studioRepository,
    IReservationRepository reservationRepository) : IRequestHandler<UpdateStudioCommand>
{
    public async Task Handle(UpdateStudioCommand request, CancellationToken cancellationToken)
    {
        var studio = await studioRepository.GetByIdAsync(request.StudioId, cancellationToken)
                     ?? throw new InvalidOperationException($"Studio '{request.StudioId}' not found.");

        studio.Update(request.Name, request.Capacity, request.HasKitchen, request.RentableAlone, request.Unavailable,
            request.RentalMode, request.NumberOfBeds);

        await studioRepository.UpdateAsync(studio, cancellationToken);

        // Switching a studio to per-bed leaves its existing (whole-lodging) reservations with a
        // bed count of 0; give them a sensible value so occupation counts them correctly.
        if (request.RentalMode == RentalMode.PerBed)
            await reservationRepository.BackfillBedCountForStudioAsync(
                request.StudioId, request.NumberOfBeds, cancellationToken);
    }
}
