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

    /// <summary>
    /// A hypothetical reservation is staked over not-yet-confirmed bookings, betting they fall
    /// through. It may therefore be placed only if it would still fit once every overlapping
    /// non-confirmed reservation is set aside — i.e. it must fit alongside the confirmed
    /// reservations alone. <paramref name="confirmedOverlaps"/> must contain only the confirmed
    /// reservations overlapping the candidate's dates on the same studio (Pending/Accepted ones
    /// are excluded). For a whole-lodging studio this means no confirmed reservation may overlap;
    /// for a per-bed studio the requested beds and people must fit within what the confirmed
    /// reservations leave free.
    /// </summary>
    public void ValidateHypotheticalAllowed(
        Studio studio,
        int requestedBeds,
        int requestedAdults,
        IReadOnlyList<Reservation> confirmedOverlaps)
    {
        bool fits;
        if (studio.IsPerBed)
        {
            var usedBeds = confirmedOverlaps.Sum(r => r.BedCount);
            var usedAdults = confirmedOverlaps.Sum(r => r.TotalAdultCount);
            fits = usedBeds + requestedBeds <= studio.NumberOfBeds
                   && usedAdults + requestedAdults <= studio.Capacity;
        }
        else
        {
            fits = confirmedOverlaps.Count == 0;
        }

        if (!fits)
            throw new ConfirmedReservationConflictException();
    }

    /// <summary>
    /// Per-bed availability rule. The candidate reservation can be accommodated only if,
    /// across every reservation already overlapping the requested dates on the same studio,
    /// the total reserved beds stay within <see cref="Studio.NumberOfBeds"/> and the total
    /// people stay within <see cref="Studio.Capacity"/>.
    /// </summary>
    public void ValidateBedAvailability(
        Studio studio,
        int requestedBeds,
        int requestedAdults,
        IReadOnlyList<Reservation> overlappingReservations)
    {
        if (!studio.IsPerBed)
            throw new InvalidOperationException(
                "Bed availability can only be checked on a per-bed studio.");

        if (requestedBeds < 1)
            throw new NoBedsAvailableException("At least one bed must be reserved.");

        if (requestedBeds > studio.NumberOfBeds)
            throw new NoBedsAvailableException(
                $"Requested {requestedBeds} bed(s) but the studio only has {studio.NumberOfBeds}.");

        var usedBeds = overlappingReservations.Sum(r => r.BedCount);
        if (usedBeds + requestedBeds > studio.NumberOfBeds)
            throw new NoBedsAvailableException(
                $"Only {studio.NumberOfBeds - usedBeds} bed(s) left for these dates, {requestedBeds} requested.");

        var usedAdults = overlappingReservations.Sum(r => r.TotalAdultCount);
        if (usedAdults + requestedAdults > studio.Capacity)
            throw new NoBedsAvailableException(
                $"Capacity exceeded: {usedAdults + requestedAdults} people for a capacity of {studio.Capacity}.");
    }

    /// <summary>
    /// Non-throwing availability test used to decide whether a hypothetical reservation can be
    /// promoted to a real one. <paramref name="realOverlaps"/> must contain only the real
    /// (non-hypothetical) reservations overlapping the candidate's dates, excluding the candidate
    /// itself. For a whole-lodging studio the candidate fits only when nothing else overlaps; for a
    /// per-bed studio it fits when reserved beds and people stay within the studio's beds and capacity.
    /// </summary>
    public bool CanAccommodate(
        Studio studio,
        Reservation candidate,
        IReadOnlyList<Reservation> realOverlaps)
    {
        if (studio.IsPerBed)
        {
            var usedBeds = realOverlaps.Sum(r => r.BedCount);
            if (usedBeds + candidate.BedCount > studio.NumberOfBeds)
                return false;

            var usedAdults = realOverlaps.Sum(r => r.TotalAdultCount);
            if (usedAdults + candidate.TotalAdultCount > studio.Capacity)
                return false;

            return true;
        }

        return realOverlaps.Count == 0;
    }
}
