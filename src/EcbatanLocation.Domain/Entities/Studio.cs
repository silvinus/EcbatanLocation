using EcbatanLocation.Domain.Enums;

namespace EcbatanLocation.Domain.Entities;

public class Studio
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!;
    public int Capacity { get; private set; }
    public bool HasKitchen { get; private set; }
    public bool RentableAlone { get; private set; }
    public bool Unavailable { get; private set; }
    public int DisplayOrder { get; private set; }

    /// <summary>How the studio is booked: as a whole lodging, or bed by bed.</summary>
    public RentalMode RentalMode { get; private set; }

    /// <summary>
    /// Number of beds available when <see cref="RentalMode"/> is <see cref="RentalMode.PerBed"/>.
    /// Always 0 in <see cref="RentalMode.PerLodging"/> mode. May be lower than <see cref="Capacity"/>
    /// (e.g. a double bed counts as one bed for two people).
    /// </summary>
    public int NumberOfBeds { get; private set; }

    public bool IsPerBed => RentalMode == RentalMode.PerBed;

    /// <summary>
    /// Number of occupancy units the studio offers: its bed count when rented per bed,
    /// otherwise its full capacity (the whole lodging counts as one occupiable block).
    /// Used by the occupation KPI so a per-bed studio is measured in beds, not capacity.
    /// </summary>
    public int OccupancyCapacity => IsPerBed ? NumberOfBeds : Capacity;

    private Studio() { }

    public static Studio Create(
        string name,
        int capacity,
        bool hasKitchen,
        bool rentableAlone,
        int displayOrder,
        bool unavailable = false,
        RentalMode rentalMode = RentalMode.PerLodging,
        int numberOfBeds = 0)
    {
        ValidateBeds(rentalMode, numberOfBeds, capacity);

        return new Studio
        {
            Id = Guid.NewGuid(),
            Name = name,
            Capacity = capacity,
            HasKitchen = hasKitchen,
            RentableAlone = rentableAlone,
            Unavailable = unavailable,
            DisplayOrder = displayOrder,
            RentalMode = rentalMode,
            NumberOfBeds = rentalMode == RentalMode.PerBed ? numberOfBeds : 0
        };
    }

    public void Update(
        string name,
        int capacity,
        bool hasKitchen,
        bool rentableAlone,
        bool unavailable,
        RentalMode rentalMode = RentalMode.PerLodging,
        int numberOfBeds = 0)
    {
        ValidateBeds(rentalMode, numberOfBeds, capacity);

        Name = name;
        Capacity = capacity;
        HasKitchen = hasKitchen;
        RentableAlone = rentableAlone;
        Unavailable = unavailable;
        RentalMode = rentalMode;
        NumberOfBeds = rentalMode == RentalMode.PerBed ? numberOfBeds : 0;
    }

    private static void ValidateBeds(RentalMode rentalMode, int numberOfBeds, int capacity)
    {
        if (rentalMode != RentalMode.PerBed)
            return;

        if (numberOfBeds < 1)
            throw new ArgumentException("A per-bed studio must have at least one bed.");
        if (numberOfBeds > capacity)
            throw new ArgumentException("The number of beds cannot exceed the studio capacity.");
    }
}
