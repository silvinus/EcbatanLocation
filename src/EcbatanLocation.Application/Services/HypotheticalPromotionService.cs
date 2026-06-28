using EcbatanLocation.Domain.Repositories;
using EcbatanLocation.Domain.Services;
using EcbatanLocation.Domain.ValueObjects;

namespace EcbatanLocation.Application.Services;

/// <summary>
/// Promotes hypothetical reservations once a slot frees up. When a real reservation on a studio is
/// removed or moved, any hypothetical staked over the freed dates is re-evaluated: if exactly one
/// now fits the studio's availability it is automatically promoted to a regular Pending reservation.
/// When several hypotheticals contend for the same freed space, none is promoted — the choice is left
/// to the owner (manual promotion by editing the reservation and clearing the hypothetical flag).
/// </summary>
public class HypotheticalPromotionService(
    IReservationRepository reservationRepository,
    IStudioRepository studioRepository,
    ReservationDomainService domainService)
{
    public async Task PromoteFittingHypotheticalsAsync(
        Guid studioId,
        DateRange freedRange,
        CancellationToken ct = default)
    {
        var studio = await studioRepository.GetByIdAsync(studioId, ct);
        if (studio is null)
            return;

        var hypotheticals = await reservationRepository.GetHypotheticalsByStudioAsync(studioId, freedRange, ct);
        if (hypotheticals.Count == 0)
            return;

        var promotable = new List<Domain.Entities.Reservation>();
        foreach (var hypothetical in hypotheticals)
        {
            // GetOverlappingByStudioAsync excludes hypotheticals, so this is the real occupancy only.
            var realOverlaps = await reservationRepository.GetOverlappingByStudioAsync(
                studioId, hypothetical.Dates, hypothetical.Id, ct);

            if (domainService.CanAccommodate(studio, hypothetical, realOverlaps))
                promotable.Add(hypothetical);
        }

        // Auto-promote only when a single candidate fits; contention is resolved manually.
        if (promotable.Count != 1)
            return;

        promotable[0].PromoteFromHypothetical();
        await reservationRepository.UpdateAsync(promotable[0], ct);
    }
}
