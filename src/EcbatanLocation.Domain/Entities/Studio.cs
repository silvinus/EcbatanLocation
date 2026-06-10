namespace EcbatanLocation.Domain.Entities;

public class Studio
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!;
    public int Capacity { get; private set; }
    public bool HasKitchen { get; private set; }
    public bool RentableAlone { get; private set; }
    public int DisplayOrder { get; private set; }

    private Studio() { }

    public static Studio Create(string name, int capacity, bool hasKitchen, bool rentableAlone, int displayOrder)
    {
        return new Studio
        {
            Id = Guid.NewGuid(),
            Name = name,
            Capacity = capacity,
            HasKitchen = hasKitchen,
            RentableAlone = rentableAlone,
            DisplayOrder = displayOrder
        };
    }
}
