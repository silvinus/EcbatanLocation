using PlanningLocation.Domain.Enums;
using PlanningLocation.Domain.ValueObjects;

namespace PlanningLocation.Domain.Entities;

public class Reservation
{
    public Guid Id { get; private set; }
    public Guid StudioId { get; private set; }
    public Guid ProprietaireId { get; private set; }
    public DateRange Dates { get; private set; } = default!;
    public string NomLocataire { get; private set; } = default!;
    public int NbAdultes { get; private set; }
    public int NbEnfantsMoins3Ans { get; private set; }
    public TypeClient TypeClient { get; private set; }
    public StatutReservation Statut { get; private set; }
    public string? AccepteePar { get; private set; }
    public DateTime? AccepteeLe { get; private set; }
    public string? ConfirmeePar { get; private set; }
    public DateTime? ConfirmeeLe { get; private set; }
    public DateTime CreeLe { get; private set; }
    public DateTime? ModifieeLe { get; private set; }

    private Reservation() { }

    public static Reservation Creer(
        Guid studioId,
        Guid proprietaireId,
        DateRange dates,
        string nomLocataire,
        int nbAdultes,
        int nbEnfantsMoins3Ans,
        TypeClient typeClient)
    {
        if (nbAdultes < 1)
            throw new ArgumentException("Au moins un adulte est requis.");
        if (nbEnfantsMoins3Ans < 0)
            throw new ArgumentException("Le nombre d'enfants ne peut pas être négatif.");
        if (string.IsNullOrWhiteSpace(nomLocataire))
            throw new ArgumentException("Le nom du locataire est requis.");

        return new Reservation
        {
            Id = Guid.NewGuid(),
            StudioId = studioId,
            ProprietaireId = proprietaireId,
            Dates = dates,
            NomLocataire = nomLocataire,
            NbAdultes = nbAdultes,
            NbEnfantsMoins3Ans = nbEnfantsMoins3Ans,
            TypeClient = typeClient,
            Statut = StatutReservation.Demande,
            CreeLe = DateTime.UtcNow
        };
    }

    public void Accepter(string parQui)
    {
        if (Statut != StatutReservation.Demande)
            throw new InvalidOperationException("Seule une réservation en 'Demande' peut être acceptée.");

        Statut = StatutReservation.Acceptee;
        AccepteePar = parQui;
        AccepteeLe = DateTime.UtcNow;
        ModifieeLe = DateTime.UtcNow;
    }

    public void Confirmer(string parQui)
    {
        if (Statut != StatutReservation.Acceptee)
            throw new InvalidOperationException("Seule une réservation 'Acceptée' peut être confirmée.");

        Statut = StatutReservation.Confirmee;
        ConfirmeePar = parQui;
        ConfirmeeLe = DateTime.UtcNow;
        ModifieeLe = DateTime.UtcNow;
    }

    public void Modifier(
        DateRange dates,
        string nomLocataire,
        int nbAdultes,
        int nbEnfantsMoins3Ans,
        TypeClient typeClient)
    {
        if (nbAdultes < 1)
            throw new ArgumentException("Au moins un adulte est requis.");
        if (nbEnfantsMoins3Ans < 0)
            throw new ArgumentException("Le nombre d'enfants ne peut pas être négatif.");

        Dates = dates;
        NomLocataire = nomLocataire;
        NbAdultes = nbAdultes;
        NbEnfantsMoins3Ans = nbEnfantsMoins3Ans;
        TypeClient = typeClient;
        ModifieeLe = DateTime.UtcNow;
    }

    public int NombreTotalPersonnes => NbAdultes + NbEnfantsMoins3Ans;
}
