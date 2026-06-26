namespace EcbatanLocation.Application.Queries.CheckOverlap;

/// <summary>
/// Availability pre-check used by the reservation form.
/// For a whole-lodging studio, only <see cref="HasConflict"/> matters.
/// For a per-bed studio, <see cref="AvailableBeds"/> and <see cref="AvailableCapacity"/>
/// report what remains free over the requested dates (excluding the edited reservation).
/// The <c>...OverConfirmed</c> fields ignore not-yet-confirmed bookings and report what only the
/// confirmed reservations leave free — used to decide whether a hypothetical reservation may be
/// staked over the slot (a hypothetical is allowed only over Pending/Accepted bookings).
/// </summary>
public record OverlapCheckResult(
    bool IsPerBed,
    bool HasConflict,
    int AvailableBeds,
    int AvailableCapacity,
    bool HasConfirmedConflict,
    int AvailableBedsOverConfirmed,
    int AvailableCapacityOverConfirmed);
