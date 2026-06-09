using PlanningLocation.Domain.Enums;

namespace PlanningLocation.Domain.Entities;

public class GrilleTarifaire
{
    public Guid Id { get; private set; }
    public int Annee { get; private set; }
    private List<LigneTarif> _lignes = [];
    public IReadOnlyCollection<LigneTarif> Lignes => _lignes.AsReadOnly();

    private GrilleTarifaire() { }

    public static GrilleTarifaire Creer(int annee, IEnumerable<LigneTarif> lignes)
    {
        return new GrilleTarifaire
        {
            Id = Guid.NewGuid(),
            Annee = annee,
            _lignes = lignes.ToList()
        };
    }

    public decimal GetTarif(TypeClient typeClient)
    {
        return _lignes.FirstOrDefault(l => l.TypeClient == typeClient)?.PrixParJourParPersonne
               ?? throw new InvalidOperationException($"Aucun tarif défini pour {typeClient} en {Annee}.");
    }
}

public class LigneTarif
{
    public Guid Id { get; private set; }
    public Guid GrilleTarifaireId { get; private set; }
    public TypeClient TypeClient { get; private set; }
    public decimal PrixParJourParPersonne { get; private set; }

    private LigneTarif() { }

    public static LigneTarif Creer(TypeClient typeClient, decimal prixParJourParPersonne)
    {
        return new LigneTarif
        {
            Id = Guid.NewGuid(),
            TypeClient = typeClient,
            PrixParJourParPersonne = prixParJourParPersonne
        };
    }
}
