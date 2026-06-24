# Phase 19 : Lien explicite parent-enfant entre réservations

**Objectif** : Remplacer la validation implicite de dépendance studio (vérifier qu'une réservation « couvrante » existe) par un **lien explicite** (`ParentReservationId`) entre une réservation « principale » (studio louable seul) et des réservations « dépendantes » (studio non louable seul).

## Règles métier

1. **Lien obligatoire** : studio `RentableAlone=false` → doit référencer une réservation parente (même propriétaire, studio indépendant, dates englobantes)
2. **Pas de chaîne** : une parente ne peut pas elle-même être une dépendante
3. **Statut couplé** : les dépendantes héritent du statut de la parente (à la création et à chaque transition). Pas de bouton Accept/Confirm sur les dépendantes
4. **Suppression cascade** : supprimer une parente supprime ses dépendantes (popup de confirmation)
5. **Dates verrouillées** : impossible de modifier les dates d'une parente qui a des dépendantes (erreur explicite)
6. **Re-liaison possible** : on peut changer la parente d'une dépendante lors de l'édition
7. **Migration douce** : colonne NULLABLE en v1, les réservations existantes restent orphelines. À terme la colonne deviendra NOT NULL

## Branche

`feat/parent-child-reservation-link` depuis `main` — 5 commits, un par sous-phase.

---

### Commit 1/5 — Domain Layer

> `feat(domain): add ParentReservationId and parent-child link logic`

#### 1.1 `Reservation.cs`

Fichier : `src/EcbatanLocation.Domain/Entities/Reservation.cs`

Ajouts :
- `public Guid? ParentReservationId { get; private set; }`
- `public bool HasParent => ParentReservationId.HasValue`
- `SetParentReservation(Guid parentId)` — affecte le lien, met à jour `UpdatedAt`
- `ClearParentReservation()` — remet à null, met à jour `UpdatedAt`
- `InheritStatus(ReservationStatus status, string? acceptedBy, DateTime? acceptedAt, string? confirmedBy, DateTime? confirmedAt)` — affecte directement `Status` et les métadonnées associées sans passer par `Accept()`/`Confirm()` (qui ont des guards de transition). Pas d'émission de domain event (la parente est le record autoritaire)

#### 1.2 `ReservationDomainService.cs`

Fichier : `src/EcbatanLocation.Domain/Services/ReservationDomainService.cs`

- **Supprimer** `ValidateStudioDependency()`
- **Ajouter** `ValidateParentLink(Studio dependentStudio, Reservation parent, Studio parentStudio, DateRange dependentDates, Guid dependentOwnerId)` :
  - `dependentStudio.RentableAlone` doit être `false`
  - `parentStudio.RentableAlone` doit être `true`
  - `parent.OwnerId == dependentOwnerId`
  - `parent.Dates.Contains(dependentDates)`
  - `parent.ParentReservationId` doit être `null` (pas de chaîne transitive)
- **Ajouter** `PropagateStatusToDependents(Reservation parent, IReadOnlyList<Reservation> dependents)` — appelle `InheritStatus()` sur chaque dépendante avec les valeurs de la parente

#### 1.3 `IReservationRepository.cs`

Fichier : `src/EcbatanLocation.Domain/Repositories/IReservationRepository.cs`

Nouvelles signatures :
```csharp
Task<IReadOnlyList<Reservation>> GetDependentsByParentIdAsync(Guid parentId, CancellationToken ct = default);
Task<bool> HasDependentsAsync(Guid reservationId, CancellationToken ct = default);
Task<IReadOnlyList<Reservation>> GetCompatibleParentsAsync(Guid ownerId, DateRange dates, Guid? excludeId = null, CancellationToken ct = default);
Task DeleteRangeAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
Task UpdateRangeAsync(IReadOnlyList<Reservation> reservations, CancellationToken ct = default);
```

---

### Commit 2/5 — Infrastructure Layer

> `feat(infra): implement parent-child FK, repository methods and migration`

#### 2.1 `ReservationConfiguration.cs`

Fichier : `src/EcbatanLocation.Infrastructure/Persistence/Configurations/ReservationConfiguration.cs`

Ajouts :
```csharp
builder.Property(r => r.ParentReservationId).IsRequired(false);
builder.HasOne<Reservation>().WithMany().HasForeignKey(r => r.ParentReservationId).OnDelete(DeleteBehavior.Restrict);
builder.HasIndex(r => r.ParentReservationId);
```

`DeleteBehavior.Restrict` = le handler applicatif gère la cascade, la BDD bloque si un oubli.

#### 2.2 `ReservationRepository.cs`

Fichier : `src/EcbatanLocation.Infrastructure/Repositories/ReservationRepository.cs`

Implémentation des 5 nouvelles méthodes :

| Méthode | Logique |
|---------|---------|
| `GetDependentsByParentIdAsync` | `Where(r => r.ParentReservationId == parentId).ToListAsync()` |
| `HasDependentsAsync` | `AnyAsync(r => r.ParentReservationId == reservationId)` |
| `GetCompatibleParentsAsync` | Join avec `Studios` → filtrer `RentableAlone == true`, même `OwnerId`, `Dates.Contains(dates)`, `ParentReservationId == null` |
| `DeleteRangeAsync` | Charger par IDs + `RemoveRange` + `SaveChangesAsync` |
| `UpdateRangeAsync` | `UpdateRange` + `SaveChangesAsync` (pas de guard overlap — les dates ne changent pas) |

#### 2.3 Migration EF Core

```bash
dotnet ef migrations add AddParentReservationLink --project src/EcbatanLocation.Infrastructure --startup-project src/EcbatanLocation.Web
```

Vérifier que la migration contient :
- `AddColumn<Guid?>("ParentReservationId")`
- `CreateIndex` sur `ParentReservationId`
- `AddForeignKey` vers `Reservations.Id` avec `onDelete: ReferentialAction.Restrict`

---

### Commit 3/5 — Application Layer

> `feat(app): wire parent-child logic in commands, queries and DTOs`

#### 3.1 Nouveaux DTOs

Fichiers à créer dans `src/EcbatanLocation.Application/DTOs/` :

- `ParentReservationOptionDto(Guid Id, string StudioName, string TenantName, DateOnly StartDate, DateOnly EndDate, ReservationStatus Status)` — alimente la liste déroulante du formulaire
- `DependentReservationSummaryDto(Guid Id, string StudioName, string TenantName, DateOnly StartDate, DateOnly EndDate, ReservationStatus Status)` — affichage dans le détail et popup de suppression

#### 3.2 DTOs modifiés

**`ReservationDetailDto`** — ajouter :
- `Guid? ParentReservationId`
- `string? ParentStudioName`
- `string? ParentTenantName`
- `IReadOnlyList<DependentReservationSummaryDto> Dependents`

Propriété calculée : `bool IsDependent => ParentReservationId.HasValue`

**`ReservationPlanningDto`** — ajouter :
- `Guid? ParentReservationId`
- `int LinkGroupIndex` (index de groupe pour le visuel calendrier, -1 = pas de groupe)

#### 3.3 Nouvelle query `GetCompatibleParentsQuery`

Fichiers à créer : `src/EcbatanLocation.Application/Queries/GetCompatibleParents/`

- `GetCompatibleParentsQuery(Guid OwnerId, DateOnly StartDate, DateOnly EndDate, Guid? ExcludeReservationId) : IRequest<IReadOnlyList<ParentReservationOptionDto>>`
- Handler : appelle `GetCompatibleParentsAsync`, joint les studios pour les noms, mappe vers DTO

#### 3.4 `CreateReservationCommand` modifié

Fichiers : `src/EcbatanLocation.Application/Commands/CreateReservation/`

- Ajouter `Guid? ParentReservationId` au command record
- Handler :
  1. Si studio `!RentableAlone` et `ParentReservationId == null` → `InvalidOperationException`
  2. Si parent fourni : charger parent + son studio → `domainService.ValidateParentLink(...)`
  3. Après `Reservation.Create(...)` : `reservation.SetParentReservation(parentId)` + `reservation.InheritStatus(parent.Status, ...)`
  4. **Supprimer** l'appel à `ValidateStudioDependency`

#### 3.5 `UpdateReservationCommand` modifié

Fichiers : `src/EcbatanLocation.Application/Commands/UpdateReservation/`

- Ajouter `Guid? ParentReservationId` au command record
- Handler :
  1. `hasDependents = await repo.HasDependentsAsync(reservation.Id)`
  2. Si dates changent ET `hasDependents` → `InvalidOperationException("Cannot change dates: this reservation has dependent reservations.")`
  3. Si studio `!RentableAlone` : valider parent link, `SetParentReservation()`
  4. Si studio `RentableAlone` : `ClearParentReservation()` (au cas où changement de studio)
  5. **Supprimer** l'appel à `ValidateStudioDependency`

#### 3.6 `DeleteReservationCommandHandler` modifié

1. Charger les dépendantes via `GetDependentsByParentIdAsync(reservation.Id)`
2. Pour chaque dépendante : `dep.MarkDeleted()`
3. `await repo.DeleteRangeAsync(dependents.Select(d => d.Id))`
4. Puis supprimer la parente (logique existante inchangée)

#### 3.7 `AcceptReservationCommandHandler` modifié

1. **Guard** : si `reservation.HasParent` → `InvalidOperationException("Dependent reservations cannot be accepted independently.")`
2. `reservation.Accept(request.AcceptedBy)` (existant)
3. Charger dépendantes → `domainService.PropagateStatusToDependents(reservation, dependents)`
4. `await repo.UpdateRangeAsync(dependents)`

Note : la propagation est **synchrone** dans le handler, pas via domain events post-commit.

#### 3.8 `ConfirmReservationCommandHandler` modifié

Même pattern que Accept :
1. Guard si dépendante
2. `reservation.Confirm(request.ConfirmedBy)`
3. Charger dépendantes + propager + sauvegarder

#### 3.9 `GetReservationDetailQueryHandler` modifié

- Si `ParentReservationId != null` : charger parent + son studio → `ParentStudioName`, `ParentTenantName`
- Charger dépendantes via `GetDependentsByParentIdAsync` + leurs studios → mapper en `DependentReservationSummaryDto`

#### 3.10 `GetMonthlyPlanningQueryHandler` modifié

- Inclure `ParentReservationId` dans le mapping vers `ReservationPlanningDto`
- Calculer `LinkGroupIndex` :
  1. Collecter tous les `ParentReservationId` non-null distincts → `parentIds`
  2. Dictionnaire `parentId → index` incrémental (0, 1, 2, ...)
  3. Pour chaque réservation :
     - Si son `Id` est dans `parentIds` → `groupMap[Id]`
     - Si son `ParentReservationId` est dans `groupMap` → `groupMap[ParentReservationId]`
     - Sinon → `-1`

---

### Commit 4/5 — UI / Blazor

> `feat(ui): parent selection dropdown, cascade delete popup, status hints and link dots`

#### 4.1 `ReservationFormModal.razor` — Liste déroulante parente

Fichier : `src/EcbatanLocation.Web/Components/Planning/ReservationFormModal.razor`

Nouveaux champs d'état :
```csharp
private Guid? _parentReservationId;
private List<ParentReservationOptionDto> _compatibleParents = [];
private bool _loadingParents;
```

- Quand le studio change vers `RentableAlone=false`, ou quand les dates changent (si studio déjà non louable seul) : appeler `GetCompatibleParentsQuery`
- Afficher un `<select>` conditionnel quand `_selectedStudio?.RentableAlone == false` :
  - Options formatées : `"StudioName — TenantName (dd/MM - dd/MM, Statut)"`
  - Si aucun résultat : message « Aucune réservation parente compatible. Créez d'abord une réservation sur un studio indépendant. »
- Validation au Save : si studio non louable seul et `_parentReservationId == null` → bloquer avec message d'erreur
- Mode édition : pré-remplir `_parentReservationId` depuis le DTO existant

#### 4.2 `ReservationDetailModal.razor` — Affichage parent/dépendantes

Fichier : `src/EcbatanLocation.Web/Components/Planning/ReservationDetailModal.razor`

- **Lien vers la parente** : si `ParentReservationId != null`, afficher une ligne « Réservation parente » avec bouton-lien cliquable (StudioName — TenantName)
- **Liste des dépendantes** : si `Dependents.Count > 0`, section « Réservations dépendantes (N) » avec liens cliquables + badge statut
- **Boutons Accept/Confirm masqués** si `IsDependent`, remplacés par : « Le statut de cette réservation suit automatiquement celui de la réservation parente. »
- **Nouveau paramètre** : `[Parameter] public EventCallback<Guid> OnReservationClicked { get; set; }` pour naviguer entre réservations liées

#### 4.3 `DeleteConfirmModal.razor` — Avertissement cascade

Fichier : `src/EcbatanLocation.Web/Components/Planning/DeleteConfirmModal.razor`

Si la réservation a des dépendantes, afficher un bloc d'alerte avant le bouton Supprimer :
```
⚠ Attention : Cette réservation a N réservation(s) dépendante(s) qui seront également supprimées :
  • StudioName — TenantName (dd/MM - dd/MM)
  • ...
```

#### 4.4 `PlanningGrid.razor` — Pastille de groupe

Fichier : `src/EcbatanLocation.Web/Components/Planning/PlanningGrid.razor`

Palette de 8 couleurs distinctes :
```csharp
private static readonly string[] LinkColors = [
    "#e74c3c", "#3498db", "#2ecc71", "#f39c12",
    "#9b59b6", "#1abc9c", "#e67e22", "#34495e"
];
```

Pour chaque réservation avec `LinkGroupIndex >= 0` : pastille ronde colorée en haut à droite de la barre de réservation.

#### 4.5 `PlanningWeekView.razor` + `PlanningListView.razor`

Même pastille de groupe adaptée au format de chaque vue.

#### 4.6 CSS — `.link-dot`

Fichier : `src/EcbatanLocation.Web/wwwroot/app.css`

```css
.link-dot {
    position: absolute;
    top: 2px;
    right: 4px;
    width: 8px;
    height: 8px;
    border-radius: 50%;
    border: 1px solid rgba(255, 255, 255, 0.6);
    pointer-events: none;
}
```

S'assurer que `.booking-span` a `position: relative`.

#### 4.7 `Home.razor.cs` — Câblage navigation

Fichier : `src/EcbatanLocation.Web/Components/Pages/Home.razor.cs`

Gérer le callback `OnReservationClicked` depuis `ReservationDetailModal` pour charger et afficher le détail d'une autre réservation (réutiliser la logique existante).

---

### Commit 5/5 — Tests

> `test: add tests for parent-child reservation link`

#### 5.1 Tests unitaires domaine

Fichier : `tests/EcbatanLocation.Domain.Tests/`

- `Reservation.SetParentReservation` : affecte correctement, met à jour `UpdatedAt`
- `Reservation.ClearParentReservation` : remet à null
- `Reservation.InheritStatus` : hérite correctement du statut Pending, Accepted, Confirmed avec métadonnées
- `ReservationDomainService.ValidateParentLink` :
  - OK si toutes conditions remplies
  - Erreur si studio parent `!RentableAlone`
  - Erreur si propriétaires différents
  - Erreur si dates non englobantes
  - Erreur si parent est lui-même une dépendante
- `ReservationDomainService.PropagateStatusToDependents` : propage correctement

#### 5.2 Tests handlers (Application)

Fichier : `tests/EcbatanLocation.Application.Tests/`

- Création avec parent : la dépendante hérite du statut
- Création sans parent sur studio non louable seul → erreur
- Update avec changement de dates sur parente avec dépendantes → erreur
- Update avec re-liaison de parent
- Delete cascade : supprime parente + dépendantes
- Accept sur parente : propage aux dépendantes
- Confirm sur parente : propage aux dépendantes
- Accept/Confirm sur dépendante → erreur

#### 5.3 Tests EF (Infrastructure)

Fichier : `tests/EcbatanLocation.Infrastructure.Tests/`

- Mapping de la colonne `ParentReservationId`
- FK Restrict : suppression bloquée si dépendantes non supprimées d'abord

---

### Vérification finale (Phase 19)

Après les 5 commits :

1. `dotnet build` — compilation sans erreur
2. `dotnet ef database update` — migration appliquée
3. `dotnet test` — tous les tests passent
4. Tester manuellement dans le navigateur :
   - [ ] Créer une réservation sur un studio indépendant
   - [ ] Créer une réservation sur un studio non louable seul → vérifier la liste déroulante, sélectionner la parente
   - [ ] Vérifier la pastille de groupe sur le calendrier
   - [ ] Accepter la parente → vérifier que la dépendante passe en Acceptée
   - [ ] Confirmer la parente → vérifier la propagation
   - [ ] Tenter de modifier les dates de la parente → vérifier le blocage
   - [ ] Supprimer la parente → vérifier le popup listant les dépendantes et la suppression cascade
   - [ ] Éditer une dépendante et changer sa parente de rattachement
   - [ ] Vérifier qu'une dépendante n'a pas de boutons Accept/Confirm
