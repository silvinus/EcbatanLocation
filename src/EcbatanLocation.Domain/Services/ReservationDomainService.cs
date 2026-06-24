using EcbatanLocation.Domain.Entities;
using EcbatanLocation.Domain.Exceptions;
using EcbatanLocation.Domain.ValueObjects;

namespace EcbatanLocation.Domain.Services;

public class ReservationDomainService
{
    public void ValidateParentLink(
        Studio dependentStudio,
        Reservation parent,
        Studio parentStudio,
        DateRange dependentDates,
        Guid dependentOwnerId)
    {
        if (dependentStudio.RentableAlone)
            throw new InvalidOperationException(
                "Only a non-rentable-alone studio can be linked to a parent reservation.");

        if (!parentStudio.RentableAlone)
            throw new InvalidOperationException(
                "The parent reservation must be on an independently rentable studio.");

        if (parent.OwnerId != dependentOwnerId)
            throw new InvalidOperationException(
                "The parent reservation must belong to the same owner.");

        if (!parent.Dates.Contains(dependentDates))
            throw new InvalidOperationException(
                "The parent reservation dates must fully contain the dependent reservation dates.");

        if (parent.ParentReservationId is not null)
            throw new InvalidOperationException(
                "The parent reservation is itself a dependent — chaining is not allowed.");
    }

    public void PropagateStatusToDependents(Reservation parent, IReadOnlyList<Reservation> dependents)
    {
        foreach (var dep in dependents)
        {
            dep.InheritStatus(
                parent.Status,
                parent.AcceptedBy,
                parent.AcceptedAt,
                parent.ConfirmedBy,
                parent.ConfirmedAt);
        }
    }

    public void ValidateNoOverlap(bool overlapExists)
    {
        if (overlapExists)
            throw new OverlappingReservationException();
    }
}
