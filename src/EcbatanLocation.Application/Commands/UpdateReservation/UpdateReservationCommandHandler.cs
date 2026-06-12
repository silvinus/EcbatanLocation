using EcbatanLocation.Application.Messaging;
using EcbatanLocation.Domain.Repositories;
using EcbatanLocation.Domain.Services;
using EcbatanLocation.Domain.ValueObjects;

namespace EcbatanLocation.Application.Commands.UpdateReservation;

public class UpdateReservationCommandHandler(
    IReservationRepository reservationRepository,
    IStudioRepository studioRepository,
    ReservationDomainService domainService) : IRequestHandler<UpdateReservationCommand>
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

        var overlapExists = await reservationRepository.ExistsOverlapAsync(
            request.StudioId, dates, request.ReservationId, cancellationToken);
        domainService.ValidateNoOverlap(overlapExists);

        if (!studio.RentableAlone)
        {
            var ownerReservations = await reservationRepository.GetByOwnerAndOverlappingDatesAsync(
                reservation.OwnerId, dates, cancellationToken);
            domainService.ValidateStudioDependency(studio, reservation.OwnerId, dates, ownerReservations);
        }

        var personLines = request.PersonLines
            .Select(pl => new PersonLine(pl.ClientType, pl.AdultCount, pl.ChildrenUnder3Count))
            .ToList();

        reservation.Update(
            dates,
            request.TenantName,
            personLines,
            studio.Capacity);

        await reservationRepository.UpdateAsync(reservation, cancellationToken);
    }
}
