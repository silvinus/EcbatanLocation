namespace EcbatanLocation.Web;

/// <summary>A single release entry shown in the "What's new" banner.</summary>
/// <param name="Version">Version string without the leading "v", e.g. "1.8.0".</param>
/// <param name="Date">Human-readable release date (dd/MM/yyyy).</param>
/// <param name="Items">User-facing highlights for this version.</param>
public record ReleaseNote(string Version, string Date, IReadOnlyList<string> Items);

/// <summary>
/// Curated, user-facing changelog displayed by <c>ReleaseBanner</c>. Kept in code so it ships
/// and is versioned with the deployment — add a new entry at the top for each release.
/// Keep entries short and written for end users, not as raw commit messages.
/// </summary>
public static class ReleaseNotes
{
    public static readonly IReadOnlyList<ReleaseNote> All =
    [
        new("1.8.0", "28/06/2026",
        [
            "Réservations « hypothétiques » et location au lit (per-bed).",
            "Impossible de poser une réservation hypothétique sur une réservation confirmée.",
            "Les actions sur une réservation sont réservées à son propriétaire ou à un admin.",
            "Un admin peut créer une réservation au nom d'un propriétaire.",
            "Correction de la navigation entre les mois en vue semaine.",
        ]),
        new("1.7.0", "24/06/2026",
        [
            "Calendrier pleine largeur, filtres en panneau latéral et bandeau de KPI.",
            "Nouvelle vue agenda optimisée pour mobile.",
            "Code couleur de disponibilité directement sur les jours du planning.",
        ]),
        new("1.6.0", "24/06/2026",
        [
            "Lien explicite parent-enfant entre réservations.",
            "Authentification renforcée (verrouillage du compte, expiration de session).",
            "Affichage de la version de l'application dans le pied de page.",
        ]),
    ];
}
