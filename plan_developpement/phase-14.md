# Phase 14 : Rapport de réservations avec calcul de prix + export PDF

**Objectif** : Fournir aux administrateurs un rapport détaillé des réservations avec calcul de prix ligne par ligne, consultable pour un mois donné ou une année complète (y compris en cours). Export PDF du rapport.

## Analyse d'impact

| Couche | Fichiers impactés | Nature de l'impact |
|--------|-------------------|--------------------|
| **Domain** | `IReservationRepository.cs` | Ajout `GetByYearAsync(int year)` pour récupérer toutes les réservations d'une année |
| **Application** | Nouveau dossier `Queries/GetReservationReport/` | Query CQRS + Handler, nouveaux DTOs `ReservationReportDto`, `ReportLineDto`, `ReportSummaryDto` |
| **Application** | `DependencyInjection.cs` | Aucun changement (auto-registration des handlers) |
| **Infrastructure** | `ReservationRepository.cs` | Implémentation de `GetByYearAsync` |
| **Web** | `Admin.razor` | Nouvel onglet « Rapport » avec filtres (mois/année), tableau détaillé, bouton export PDF |
| **Web** | `EcbatanLocation.Web.csproj` | Ajout package QuestPDF Community |
| **Web** | Nouveau `Services/ReportPdfGenerator.cs` | Génération du document PDF via QuestPDF |
| **Tests** | Application.Tests | Tests du handler de rapport |

## Fonctionnalités du rapport

1. **Filtrage** :
   - Par **mois** (mois + année) : affiche les réservations qui chevauchent ce mois
   - Par **année complète** : affiche toutes les réservations de l'année, même si l'année n'est pas terminée (encours)
   - Choix via toggle « Mois » / « Année »

2. **Tableau détaillé** :
   - Studio, Locataire, Propriétaire, Dates, Nb jours, Lignes de personnes (type, adultes, enfants -3 ans), Tarif unitaire, Montant ligne, Montant total réservation, Statut
   - Tri par date de début

3. **Synthèse** :
   - Nombre total de réservations
   - Nombre total de nuitées
   - Montant total (toutes réservations)
   - Ventilation par statut (Demande / Acceptée / Confirmée)
   - Ventilation par propriétaire

4. **Export PDF** :
   - Bouton « Exporter PDF » génère et télécharge un document PDF formaté
   - En-tête : titre, période, date de génération
   - Corps : tableau des réservations avec détail prix
   - Pied : synthèse et totaux

## Tâches

### 1. Domain — Repository

- **`IReservationRepository.cs`** — Ajouter : `Task<IReadOnlyList<Reservation>> GetByYearAsync(int year, CancellationToken ct = default)`.

### 2. Infrastructure — Repository

- **`ReservationRepository.cs`** — Implémenter `GetByYearAsync` : filtre les réservations dont les dates chevauchent l'année (StartDate < 1er janvier N+1 ET EndDate > 1er janvier N).

### 3. Application — DTOs

- **`ReportLineDto`** : `ReservationId`, `StudioName`, `OwnerName`, `TenantName`, `StartDate`, `EndDate`, `NumberOfDays`, `PersonLines` (type, adultes, enfants, tarif unitaire, montant ligne), `TotalAmount`, `Status`.
- **`ReportPersonLineDto`** : `ClientTypeLabel`, `AdultCount`, `ChildrenUnder3Count`, `RatePerDay`, `LineAmount`.
- **`ReportSummaryDto`** : `TotalReservations`, `TotalNights`, `TotalAmount`, `ByStatus` (dict statut→montant+count), `ByOwner` (dict owner→montant+count).
- **`ReservationReportDto`** : `Year`, `Month?`, `PeriodLabel`, `Lines`, `Summary`, `GeneratedAt`.

### 4. Application — Query

- **`GetReservationReportQuery`** (`IRequireAdmin`) : `int Year`, `int? Month`.
- **`GetReservationReportQueryHandler`** :
  - Si `Month` est fourni → `GetByMonthAsync(year, month)`, sinon → `GetByYearAsync(year)`.
  - Charge la `PricingGrid` de l'année.
  - Pour chaque réservation : calcule le montant par ligne de personnes via `PricingGrid.CalculateAmount()`.
  - Charge studios et owners pour les noms.
  - Construit le `ReservationReportDto` avec lignes triées par date + synthèse.

### 5. Web — Onglet Rapport

- **`Admin.razor`** — Ajouter un 5e onglet « Rapport » :
  - Toggle « Mois » / « Année » pour choisir la granularité.
  - Sélecteur mois + année (avec navigation ← →).
  - Bouton « Générer le rapport ».
  - Tableau des réservations avec détail prix.
  - Section synthèse (totaux, ventilation par statut et par propriétaire).
  - Bouton « Exporter PDF » (téléchargement via JS interop).

### 6. Web — Génération PDF

- **Package** : ajouter `QuestPDF` au projet Web (licence Community, gratuit pour revenus < $1M).
- **`Services/ReportPdfGenerator.cs`** : service injectable qui prend un `ReservationReportDto` et retourne un `byte[]` PDF.
  - En-tête : logo/titre Ecbatan Location, période, date de génération.
  - Tableau : colonnes Studio, Locataire, Propriétaire, Dates, Jours, Détail personnes, Montant, Statut.
  - Synthèse : totaux, ventilations.
- **Endpoint** : route `/api/report/pdf?year=2026&month=7` protégée `[Authorize(Roles="Admin")]` retournant le fichier PDF.

### 7. Tests

- **Application.Tests** : `GetReservationReportQueryHandler` — rapport mensuel, rapport annuel, calcul correct des montants, synthèse correcte, période sans réservations.

## Points d'attention

- **Réservations à cheval sur la période** : une réservation du 28 juin au 5 juillet apparaît dans les rapports de juin ET de juillet. Le montant affiché est le montant **total** de la réservation (pas un prorata).
- **Grille tarifaire manquante** : si aucune grille n'existe pour l'année, les montants sont affichés comme « N/A » et la synthèse les exclut.
- **QuestPDF Community** : licence gratuite pour usage non commercial ou revenus < $1M/an. Parfait pour ce projet.

**Livrable** : Onglet rapport dans l'admin avec tableau détaillé des réservations, calcul de prix, synthèse, et export PDF.
