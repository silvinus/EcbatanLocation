# Phase 21 : Location au lit (per-bed) pour certains logements

**Objectif** : Permettre, pour certains logements, de réserver **place par place (au lit)** plutôt que le logement entier. Un logement « au lit » peut accueillir **plusieurs réservations simultanées** sur des dates qui se croisent, tant que la somme des lits réservés reste dans le nombre de lits et la somme des personnes dans la capacité.

Exemple : Studio Centre = 4 places / 4 lits → jusqu'à 4 réservations d'une personne sur la même plage.

## Règles métier

1. **Mode de location par logement** : `RentalMode ∈ { PerLodging, PerBed }`. `PerLodging` = comportement historique (libre ou occupé, aucun chevauchement). `PerBed` = réservation au lit.
2. **Nombre de lits** : un logement `PerBed` porte un `NumberOfBeds` (≥ 1, ≤ `Capacity`). Il peut être **inférieur à la capacité** (ex. 1 lit double + 2 lits simples = 3 lits pour une capacité de 4).
3. **Lits par réservation** : une réservation sur un logement `PerBed` consomme un `BedCount` (≥ 1).
4. **Disponibilité concurrente** (logement `PerBed`, dates qui se croisent) :
   - Σ `BedCount` ≤ `NumberOfBeds`
   - Σ adultes ≤ `Capacity`
5. **Saisie des lits non forcée** : si `Capacity == NumberOfBeds` (1 lit = 1 personne), on ne demande pas le nombre de lits, on prend le **nombre de personnes** (`BedCount` = adultes). Sinon (lits < capacité), on demande le nombre de lits.
6. **Coexistence avec le lien parent (H1 / Phase 19)** : le réglage « au lit » et « louable seul » sont **indépendants**. Un logement `PerBed` non louable seul reste rattaché à une réservation parente (chaque réservation-lit est validée indépendamment). Un même parent peut couvrir plusieurs réservations-lit sur le même logement.
7. **Occupation (H2)** : un logement `PerLodging` occupé compte sa **capacité** entière ; un logement `PerBed` compte le **nombre de lits occupés** (Σ `BedCount` des réservations Acceptée/Confirmée du jour), borné au nombre de lits. Le total d'occupation utilise aussi les lits pour les logements `PerBed`.
8. **Backfill** : les réservations existantes d'un logement basculé en `PerBed` ont `BedCount = 0` ; elles sont corrigées (= nombre d'adultes, borné `[1, NumberOfBeds]`), au changement de mode et via un one-shot idempotent au démarrage.

## Branche

`feat/per-bed-rental` depuis `main`.

---

### Commit 1 — Domain Layer

> `feat(domain): add per-bed rental mode, bed count and availability rule`

#### 1.1 `RentalMode.cs` (nouveau)

Fichier : `src/EcbatanLocation.Domain/Enums/RentalMode.cs`

Enum `{ PerLodging, PerBed }`.

#### 1.2 `NoBedsAvailableException.cs` (nouveau)

Fichier : `src/EcbatanLocation.Domain/Exceptions/NoBedsAvailableException.cs`

Levée quand un logement `PerBed` ne peut pas accueillir la demande (lits ou personnes insuffisants).

#### 1.3 `Studio.cs`

Fichier : `src/EcbatanLocation.Domain/Entities/Studio.cs`

Ajouts :
- `RentalMode RentalMode` + `int NumberOfBeds`
- `bool IsPerBed => RentalMode == RentalMode.PerBed`
- `int OccupancyCapacity => IsPerBed ? NumberOfBeds : Capacity` (unité d'occupation pour le KPI)
- `Create(...)` / `Update(...)` reçoivent `rentalMode` + `numberOfBeds` (optionnels, défaut `PerLodging`/0)
- Invariant `ValidateBeds` : `PerBed ⇒ 1 ≤ NumberOfBeds ≤ Capacity`. En `PerLodging`, `NumberOfBeds` forcé à 0.

#### 1.4 `Reservation.cs`

Fichier : `src/EcbatanLocation.Domain/Entities/Reservation.cs`

Ajouts :
- `int BedCount` (0 pour une réservation au logement entier)
- `Create(...)` / `Update(...)` reçoivent `rentalMode`, `studioBeds`, `bedCount` → `NormalizeBedCount` valide `1 ≤ bedCount ≤ studioBeds` en `PerBed`, sinon 0
- `BackfillBedCount(int bedCount)` — affecte `BedCount = max(1, bedCount)` sans repasser par la validation (correction de données)

#### 1.5 `ReservationDomainService.cs`

Fichier : `src/EcbatanLocation.Domain/Services/ReservationDomainService.cs`

`ValidateBedAvailability(Studio studio, int requestedBeds, int requestedAdults, IReadOnlyList<Reservation> overlapping)` :
- exige `studio.IsPerBed`
- `requestedBeds ≥ 1` et `≤ NumberOfBeds`
- Σ lits occupants + `requestedBeds` ≤ `NumberOfBeds`
- Σ adultes occupants + `requestedAdults` ≤ `Capacity`
- erreurs → `NoBedsAvailableException`

`ValidateNoOverlap(bool)` (existant) reste pour les logements `PerLodging`.

#### 1.6 `IReservationRepository.cs`

Fichier : `src/EcbatanLocation.Domain/Repositories/IReservationRepository.cs`

Nouvelles signatures :
```csharp
Task<IReadOnlyList<Reservation>> GetOverlappingByStudioAsync(Guid studioId, DateRange dates, Guid? excludeReservationId = null, CancellationToken ct = default);
Task<int> BackfillBedCountForStudioAsync(Guid studioId, int numberOfBeds, CancellationToken ct = default);
```

---

### Commit 2 — Infrastructure Layer

> `feat(infra): per-bed columns, mode-aware availability guard and migrations`

#### 2.1 `StudioConfiguration.cs`

Fichier : `src/EcbatanLocation.Infrastructure/Persistence/Configurations/StudioConfiguration.cs`

```csharp
builder.Property(s => s.RentalMode).IsRequired().HasConversion<string>().HasMaxLength(20)
    .HasDefaultValue(RentalMode.PerLodging);
builder.Property(s => s.NumberOfBeds).IsRequired().HasDefaultValue(0);
builder.Ignore(s => s.IsPerBed);
builder.Ignore(s => s.OccupancyCapacity);
```

#### 2.2 `ReservationConfiguration.cs`

Fichier : `src/EcbatanLocation.Infrastructure/Persistence/Configurations/ReservationConfiguration.cs`

```csharp
builder.Property(r => r.BedCount).IsRequired().HasDefaultValue(0);
```

#### 2.3 `ReservationRepository.cs`

Fichier : `src/EcbatanLocation.Infrastructure/Repositories/ReservationRepository.cs`

- `GetOverlappingByStudioAsync` : réservations du studio croisant la plage (option d'exclusion).
- `GuardNoOverlapAsync` rendu **mode-aware** (garde authoritaire dans la transaction d'écriture) :
  - `PerLodging` → tout chevauchement interdit (comportement existant).
  - `PerBed` → charge le studio + les réservations croisantes, vérifie Σ lits ≤ `NumberOfBeds` et Σ adultes ≤ `Capacity`, sinon `NoBedsAvailableException`.
- `BackfillBedCountForStudioAsync` : réservations du studio avec `BedCount == 0` → `BackfillBedCount(min(max(1, adultes), numberOfBeds))`.

> Note : l'index unique `IX_Reservations_StudioId_StartDate_EndDate` évoqué dans les commentaires n'existe pas réellement (ni config EF ni migration) ; la protection anti-chevauchement est 100 % applicative, donc aucune contrainte SQL à démonter pour le mode au lit.

#### 2.4 `DbInitializer.cs`

Fichier : `src/EcbatanLocation.Infrastructure/Persistence/DbInitializer.cs`

`BackfillPerBedBedCountsAsync` appelée au démarrage (après seeding) : pour chaque logement `PerBed`, met `BedCount` (= adultes borné aux lits) sur ses réservations à 0. Idempotent.

#### 2.5 Migrations EF Core (2 providers)

```bash
# SQLite
dotnet ef migrations add AddPerBedRental --project src/EcbatanLocation.Infrastructure.Migrations.Sqlite --startup-project src/EcbatanLocation.Web   # DatabaseProvider=Sqlite
# PostgreSQL
dotnet ef migrations add AddPerBedRental --project src/EcbatanLocation.Infrastructure.Migrations.PostgreSQL --startup-project src/EcbatanLocation.Web  # DatabaseProvider=PostgreSQL
```

Colonnes ajoutées : `Studios.RentalMode` (défaut `'PerLodging'`), `Studios.NumberOfBeds` (défaut 0), `Reservations.BedCount` (défaut 0). L'existant reste en `PerLodging`.

---

### Commit 3 — Application Layer

> `feat(app): wire per-bed mode in commands, queries, DTOs and occupation`

#### 3.1 DTOs modifiés

- **`StudioDto`** — + `RentalMode RentalMode`, `int NumberOfBeds` (optionnels en fin de record).
- **`ReservationPlanningDto`** — + `int BedCount`.
- **`ReservationDetailDto`** — + `int BedCount`.

#### 3.2 Studio (admin)

Fichiers : `Commands/CreateStudio/`, `Commands/UpdateStudio/`

- Commands + `RentalMode RentalMode`, `int NumberOfBeds`.
- Validators : si `PerBed`, `1 ≤ NumberOfBeds ≤ Capacity`.
- `CreateStudioCommandHandler` / `UpdateStudioCommandHandler` transmettent mode + lits.
- `UpdateStudioCommandHandler` : après mise à jour, si `PerBed` → `BackfillBedCountForStudioAsync` (les anciennes résas du studio passent à un `BedCount` cohérent).

#### 3.3 Réservation

Fichiers : `Commands/CreateReservation/`, `Commands/UpdateReservation/`

- Commands + `int BedCount = 1`.
- Handlers : branchement sur `studio.IsPerBed`
  - `PerBed` → `GetOverlappingByStudioAsync` + `ValidateBedAvailability(studio, BedCount, adultes, overlapping)`
  - sinon → `ExistsOverlapAsync` + `ValidateNoOverlap`
  - `Reservation.Create/Update(...)` reçoivent `studio.RentalMode`, `studio.NumberOfBeds`, `request.BedCount`.

#### 3.4 Pré-contrôle UI `CheckOverlap`

Fichiers : `Queries/CheckOverlap/`

- Nouveau `OverlapCheckResult(bool IsPerBed, bool HasConflict, int AvailableBeds, int AvailableCapacity)`.
- `CheckOverlapQuery` renvoie désormais ce résultat (au lieu d'un `bool`).
- Handler : pour un logement `PerBed`, calcule lits/places libres sur la plage (hors réservation éditée).

#### 3.5 Occupation (H2)

Fichiers : `Queries/GetDailyOccupation/`, `Queries/GetRangeOccupation/`

- Total = Σ `OccupancyCapacity` (lits pour `PerBed`, capacité sinon).
- Occupé par studio actif (Acceptée/Confirmée) : `PerBed` → `min(Σ BedCount, NumberOfBeds)` ; sinon `Capacity`.

#### 3.6 Mappings

- `GetStudiosQueryHandler`, `GetMonthlyPlanningQueryHandler`, `GetReservationDetailQueryHandler` : propagent `RentalMode`/`NumberOfBeds`/`BedCount` dans les DTOs.

---

### Commit 4 — UI / Blazor

> `feat(ui): per-bed studio config, bed field, stacked planning lanes`

#### 4.1 `Admin.razor` — Configuration du logement

Fichier : `src/EcbatanLocation.Web/Components/Pages/Admin.razor`

- Case « Louer au lit » + champ « Nombre de lits » (visible si coché, `max = capacité`).
- Colonne « Location » dans le tableau des studios (badge « Au lit (N) » / « Logement »).
- `SaveStudio` envoie `RentalMode` + `NumberOfBeds`.

#### 4.2 `ReservationFormModal.razor` — Saisie des lits

Fichier : `src/EcbatanLocation.Web/Components/Planning/ReservationFormModal.razor`

- `IsPerBedStudio`, `NeedsBedInput` (= `PerBed` ET `Capacity != NumberOfBeds`), `RequestedBeds` (= `_bedCount` si saisie, sinon nombre d'adultes).
- Champ « Nombre de lits réservés » affiché **uniquement si `NeedsBedInput`** ; sinon mention « Logement loué au lit (1 lit par personne) » + lits restants.
- Pré-contrôle `OverlapCheckResult` : `RecomputeConflict()` calcule le dépassement (lits / places) sans requête supplémentaire (recalcul sur changement de lits/adultes).
- `Save` envoie `BedCount = RequestedBeds`.
- En-tête de colonnes « Adultes » / « Enfants <3 ans » aligné à gauche au-dessus des champs (voir CSS 4.5).

#### 4.3 `PlanningGrid.razor` — Barres empilées

Fichier : `src/EcbatanLocation.Web/Components/Planning/PlanningGrid.razor`

- `BuildLanes(studioPlan)` : pour un logement `PerBed`, packing des réservations concurrentes en **lanes** (partition d'intervalles greedy, jour de départ exclusif). Logement entier = une seule lane.
- Rendu : une `<tr>` par lane, cellule studio en `rowspan` sur la 1ʳᵉ lane ; méta « N lits » ; badge « N lit(s) » sur la barre.

#### 4.4 `ReservationDetailModal.razor`

Fichier : `src/EcbatanLocation.Web/Components/Planning/ReservationDetailModal.razor`

- Ligne « Lits réservés » affichée si `BedCount > 0`.

#### 4.5 CSS — `app.css`

Fichier : `src/EcbatanLocation.Web/wwwroot/app.css`

- Hauteur des lignes du planning resserrée (`.planning td` : `height` 90px → 40px, padding vertical réduit, `vertical-align: middle`) + typo de `.studio-name` / `.studio-meta` resserrée.
- En-tête de colonnes des personnes : `.person-line-head` (mêmes `flex` que la ligne de saisie, `min-width:0` pour ne pas élargir la colonne « Enfants <3 ans », libellés `.col-head` alignés à gauche).

---

### Commit 5 — Tests

> `test: add per-bed availability, repository guard, occupation and backfill tests`

#### 5.1 Domaine — `tests/EcbatanLocation.Domain.Tests/Services/PerBedAvailabilityTests.cs`

- `ValidateBedAvailability` : OK dans les lits/capacité, dépassement lits, dépassement capacité, demande > total lits, studio non `PerBed` → erreur.
- `Reservation.Create` `PerBed` : `BedCount` > lits → erreur, 0 lit → erreur, `PerLodging` → `BedCount` 0.
- `Studio.Create` : lits > capacité → erreur, `PerLodging` force lits 0.

#### 5.2 Infrastructure — `tests/EcbatanLocation.Infrastructure.Tests/Persistence/PerBedReservationRepositoryTests.cs`

- Réservations concurrentes dans la limite des lits → acceptées.
- Dépassement lits / capacité → `NoBedsAvailableException`.
- Logement entier : chevauchement → `OverlappingReservationException` (inchangé).
- `BackfillBedCountForStudioAsync` : met les adultes (borné aux lits) sur les `BedCount == 0`, laisse les autres inchangées.

#### 5.3 Application — `tests/EcbatanLocation.Application.Tests/Queries/PerBedOccupationTests.cs`

- Occupation jour d'un logement `PerBed` = lits occupés (pas la capacité entière).
- Les réservations en *Demande* ne comptent pas.

---

### Vérification finale (Phase 21)

1. `dotnet build EcbatanLocation.slnx` — compilation sans erreur.
2. `dotnet ef database update` (les 2 providers) — migration `AddPerBedRental` appliquée.
3. `dotnet test` — tous les tests passent.
4. Tester manuellement dans le navigateur :
   - [ ] Configurer un logement en « au lit » (admin) avec N lits, puis < capacité (lits doubles).
   - [ ] Réserver plusieurs lits sur la même plage → barres empilées sur le planning.
   - [ ] Bloquer une réservation qui dépasse les lits / la capacité restante.
   - [ ] Logement où capacité = lits → pas de champ « nombre de lits », `BedCount` = nb de personnes.
   - [ ] KPI « Places occupées » : un logement au lit compte les lits occupés, pas la capacité entière.
   - [ ] Basculer un logement existant en « au lit » → ses réservations existantes obtiennent un `BedCount` cohérent (backfill).

---

### Notes / décisions

- **Unité d'occupation mixte** : le KPI mélange « places » (logements entiers) et « lits » (logements au lit). Volontaire et conforme à la demande (« compter les lits occupés »).
- **Lits non modélisés individuellement** : seuls les agrégats `NumberOfBeds` + `Capacity` sont stockés (pas de distinction lit simple/double nommée).
- **Backfill** : valeur retenue = nombre d'adultes de la réservation (borné `[1, NumberOfBeds]`), plus précis qu'un `1` fixe tout en restant ≥ 1.
