namespace PlanningLocation.Domain.Entities;

public class Studio
{
    public Guid Id { get; private set; }
    public string Nom { get; private set; } = default!;
    public int Capacite { get; private set; }
    public bool ACuisine { get; private set; }
    public bool LouableSeul { get; private set; }
    public int OrdreAffichage { get; private set; }

    private Studio() { }

    public static Studio Creer(string nom, int capacite, bool aCuisine, bool louableSeul, int ordreAffichage)
    {
        return new Studio
        {
            Id = Guid.NewGuid(),
            Nom = nom,
            Capacite = capacite,
            ACuisine = aCuisine,
            LouableSeul = louableSeul,
            OrdreAffichage = ordreAffichage
        };
    }
}
