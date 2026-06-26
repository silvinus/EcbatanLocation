using EcbatanLocation.Application.Messaging;
using EcbatanLocation.Domain.Entities;
using EcbatanLocation.Domain.Repositories;
using EcbatanLocation.Domain.Services;
using EcbatanLocation.Domain.ValueObjects;

namespace EcbatanLocation.Application.Commands.CreateReservation;

public class CreateReservationCommandHandler(
    IReservationRepository reservationRepository,
    IStudioRepository studioRepository,
    ReservationDomainService domainService) : IRequestHandler<CreateReservationCommand, Guid>
{
    public async Task<Guid> Handle(CreateReservationCommand request, CancellationToken cancellationToken)
    {
        var studio = await studioRepository.GetByIdAsync(request.StudioId, cancellationToken)
                     ?? throw new InvalidOperationException($"Studio '{request.StudioId}' not found.");

        if (studio.Unavailable)
            throw new InvalidOperationException($"Studio '{studio.Name}' is currently unavailable and cannot be reserved.");

        var dates = new DateRange(request.StartDate, request.EndDate);

        var requestedAdults = request.PersonLines.Sum(pl => pl.AdultCount);

        // A hypothetical reservation is deliberately staked over an already-taken slot, so it skips
        // the availability checks (overlap / beds-capacity) entirely.
        if (!request.IsHypothetical)
        {
            if (studio.IsPerBed)
            {
                var overlapping = await reservationRepository.GetOverlappingByStudioAsync(
                    request.StudioId, dates, null, cancellationToken);
                domainService.ValidateBedAvailability(studio, request.BedCount, requestedAdults, overlapping);
            }
            else
            {
                var overlapExists = await reservationRepository.ExistsOverlapAsync(
                    request.StudioId, dates, null, cancellationToken);
                domainService.ValidateNoOverlap(overlapExists);
            }
        }

        Reservation? parent = null;
        if (!studio.RentableAlone)
        {
            if (request.ParentReservationId is null)
                throw new InvalidOperationException(
                    $"Studio '{studio.Name}' is not rentable alone. A parent reservation must be specified.");

            parent = await reservationRepository.GetByIdAsync(request.ParentReservationId.Value, cancellationToken)
                     ?? throw new InvalidOperationException("Parent reservation not found.");

            var parentStudio = await studioRepository.GetByIdAsync(parent.StudioId, cancellationToken)
                               ?? throw new InvalidOperationException("Parent studio not found.");

            domainService.ValidateParentLink(studio, parent, parentStudio, dates, request.OwnerId);
        }

        var personLines = request.PersonLines
            .Select(pl => new PersonLine(pl.ClientType, pl.AdultCount, pl.ChildrenUnder3Count))
            .ToList();

        var reservation = Reservation.Create(
            request.StudioId,
            request.OwnerId,
            dates,
            request.TenantName,
            personLines,
            studio.Capacity,
            studio.RentalMode,
            studio.NumberOfBeds,
            request.BedCount,
            request.IsHypothetical);

        if (parent is not null)
        {
            reservation.SetParentReservation(parent.Id);
            reservation.InheritStatus(
                parent.Status,
                parent.AcceptedBy,
                parent.AcceptedAt,
                parent.ConfirmedBy,
                parent.ConfirmedAt);
        }

        await reservationRepository.AddAsync(reservation, cancellationToken);

        return reservation.Id;
    }
}
