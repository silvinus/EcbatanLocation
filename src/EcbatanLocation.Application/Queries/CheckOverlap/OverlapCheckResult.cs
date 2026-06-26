namespace EcbatanLocation.Application.Queries.CheckOverlap;

/// <summary>
/// Availability pre-check used by the reservation form.
/// For a whole-lodging studio, only <see cref="HasConflict"/> matters.
/// For a per-bed studio, <see cref="AvailableBeds"/> and <see cref="AvailableCapacity"/>
/// report what remains free over the requested dates (excluding the edited reservation).
/// </summary>
public record OverlapCheckResult(
    bool IsPerBed,
    bool HasConflict,
    int AvailableBeds,
    int AvailableCapacity);
