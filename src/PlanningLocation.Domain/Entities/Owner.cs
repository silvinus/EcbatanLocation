namespace PlanningLocation.Domain.Entities;

public class Owner
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!;
    public string UserId { get; private set; } = default!;

    private Owner() { }

    public static Owner Create(string name, string userId)
    {
        return new Owner
        {
            Id = Guid.NewGuid(),
            Name = name,
            UserId = userId
        };
    }
}
