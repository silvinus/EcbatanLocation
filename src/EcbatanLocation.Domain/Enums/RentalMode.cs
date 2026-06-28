namespace EcbatanLocation.Domain.Enums;

/// <summary>
/// How a studio is booked.
/// <see cref="PerLodging"/>: the whole studio is reserved at once (free or occupied, no partial booking).
/// <see cref="PerBed"/>: several reservations can share the studio on overlapping dates,
/// as long as the sum of reserved beds stays within <c>NumberOfBeds</c> and the sum of
/// people stays within <c>Capacity</c>.
/// </summary>
public enum RentalMode
{
    PerLodging,
    PerBed
}
