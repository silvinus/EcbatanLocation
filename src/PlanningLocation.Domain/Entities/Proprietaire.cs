namespace PlanningLocation.Domain.Entities;

public class Proprietaire
{
    public Guid Id { get; private set; }
    public string Nom { get; private set; } = default!;
    public string UserId { get; private set; } = default!;

    private Proprietaire() { }

    public static Proprietaire Creer(string nom, string userId)
    {
        return new Proprietaire
        {
            Id = Guid.NewGuid(),
            Nom = nom,
            UserId = userId
        };
    }
}
