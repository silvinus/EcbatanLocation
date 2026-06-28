using EcbatanLocation.Application.Messaging;
using EcbatanLocation.Application.Services;
using EcbatanLocation.Domain.Enums;
using EcbatanLocation.Domain.Repositories;
using EcbatanLocation.Domain.Services;
using EcbatanLocation.Domain.ValueObjects;

namespace EcbatanLocation.Application.Commands.UpdateReservation;

public class UpdateReservationCommandHandler(
    IReservationRepository reservationRepository,
    IStudioRepository studioRepository,
    ReservationDomainService domainService,
    HypotheticalPromotionService promotionService) : IRequestHandler<UpdateReservationCommand>
{
    public async Task Handle(UpdateReservationCommand request, CancellationToken cancellationToken)
    {
        var reservation = await reservationRepository.GetByIdAsync(request.ReservationId, cancellationToken)
                          ?? throw new InvalidOperationException($"Reservation '{request.ReservationId}' not found.");

        var studio = await studioRepository.GetByIdAsync(request.StudioId, cancellationToken)
                     ?? throw new InvalidOperationException($"Studio '{request.StudioId}' not found.");

        if (studio.Unavailable)
            throw new InvalidOperationException($"Studio '{studio.Name}' is currently unavailable and cannot be reserved.");

        var dates = new DateRange(request.StartDate, request.EndDate);

        // Snapshot the slot occupied before this edit: if it was a real reservation, moving or
        // shrinking it (or unticking the hypothetical flag elsewhere) may free space for a hypothetical.
        var freedByReal = !reservation.IsHypothetical;
        var freedStudioId = reservation.StudioId;
        var freedRange = reservation.Dates;

        var requestedAdults = request.PersonLines.Sum(pl => pl.AdultCount);

        // A hypothetical reservation skips the regular availability checks but may only be staked over
        // a not-yet-confirmed booking; a real one (including a hypothetical being promoted by unticking
        // the flag) must still fit.
        if (!request.IsHypothetical)
        {
            if (studio.IsPerBed)
            {
                var overlapping = await reservationRepository.GetOverlappingByStudioAsync(
                    request.StudioId, dates, request.ReservationId, cancellationToken);
                domainService.ValidateBedAvailability(studio, request.BedCount, requestedAdults, overlapping);
            }
            else
            {
                var overlapExists = await reservationRepository.ExistsOverlapAsync(
                    request.StudioId, dates, request.ReservationId, cancellationToken);
                domainService.ValidateNoOverlap(overlapExists);
            }
        }
        else
        {
            var overlapping = await reservationRepository.GetOverlappingByStudioAsync(
                request.StudioId, dates, request.ReservationId, cancellationToken);
            var confirmedOverlaps = overlapping.Where(r => r.Status == ReservationStatus.Confirmed).ToList();
            domainService.ValidateHypotheticalAllowed(studio, request.BedCount, requestedAdults, confirmedOverlaps);
        }

        var hasDependents = await reservationRepository.HasDependentsAsync(reservation.Id, cancellationToken);
        if (hasDependents && (dates.StartDate != reservation.Dates.StartDate || dates.EndDate != reservation.Dates.EndDate))
            throw new InvalidOperationException(
                "Cannot change dates: this reservation has dependent reservations.");

        if (!studio.RentableAlone)
        {
            if (request.ParentReservationId is null)
                throw new InvalidOperationException(
                    $"Studio '{studio.Name}' is not rentable alone. A parent reservation must be specified.");

            var parent = await reservationRepository.GetByIdAsync(request.ParentReservationId.Value, cancellationToken)
                         ?? throw new InvalidOperationException("Parent reservation not found.");

            var parentStudio = await studioRepository.GetByIdAsync(parent.StudioId, cancellationToken)
                               ?? throw new InvalidOperationException("Parent studio not found.");

            domainService.ValidateParentLink(studio, parent, parentStudio, dates, reservation.OwnerId);
            reservation.SetParentReservation(parent.Id);
        }
        else
        {
            reservation.ClearParentReservation();
        }

        var personLines = request.PersonLines
            .Select(pl => new Domain.ValueObjects.PersonLine(pl.ClientType, pl.AdultCount, pl.ChildrenUnder3Count))
            .ToList();

        reservation.Update(
            dates,
            request.TenantName,
            personLines,
            studio.Capacity,
            studio.RentalMode,
            studio.NumberOfBeds,
            request.BedCount,
            request.IsHypothetical);

        await reservationRepository.UpdateAsync(reservation, cancellationToken);

        // If a real reservation was moved/shrunk, a hypothetical staked over the vacated slot may now fit.
        if (freedByReal)
            await promotionService.PromoteFittingHypotheticalsAsync(freedStudioId, freedRange, cancellationToken);
    }
}
