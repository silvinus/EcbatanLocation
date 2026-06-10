using PlanningLocation.Domain.Entities;
using PlanningLocation.Domain.Exceptions;
using PlanningLocation.Domain.ValueObjects;

namespace PlanningLocation.Domain.Services;

public class ReservationDomainService
{
    public void ValidateStudioDependency(
        Studio studio,
        Guid ownerId,
        DateRange dates,
        IReadOnlyList<Reservation> ownerReservations)
    {
        if (studio.RentableAlone)
            return;

        var hasIndependentStudio = ownerReservations.Any(r =>
            r.OwnerId == ownerId
            && r.StudioId != studio.Id
            && r.Dates.Overlaps(dates));

        if (!hasIndependentStudio)
            throw new InvalidOperationException(
                $"Studio '{studio.Name}' cannot be rented alone. " +
                "A reservation on an independent studio with overlapping dates is required.");
    }

    public void ValidateNoOverlap(bool overlapExists)
    {
        if (overlapExists)
            throw new OverlappingReservationException();
    }
}
