# Phase 13 : Gestion admin des studios + champ Indisponible

**Objectif** : Rendre le catalogue de studios modifiable par un administrateur (nom, capacité, cuisine, louable seul) et ajouter un champ **Indisponible** (`Unavailable`) permettant de verrouiller un logement pour empêcher toute nouvelle réservation.

## Analyse d'impact

| Couche | Fichiers impactés | Nature de l'impact |
|--------|-------------------|--------------------|
| **Domain** | `Studio.cs`, `IStudioRepository.cs` | Propriété `Unavailable`, méthode `Update()`, `UpdateAsync` au repository |
| **Application** | `StudioDto.cs`, nouveaux `Commands/UpdateStudio/*` | Champ `Unavailable` au DTO, commande CQRS `UpdateStudio` |
| **Application** | `CreateReservationCommandHandler`, `UpdateReservationCommandHandler` | Guard si `studio.Unavailable` |
| **Application** | 5 query handlers (studios, planning, detail, occupation) | Propagation `Unavailable` dans le DTO |
| **Infrastructure** | `StudioConfiguration.cs`, `StudioRepository.cs`, `DbInitializer.cs` | Config EF Core, `UpdateAsync`, seed, migration |
| **Web** | `Admin.razor` | Onglet Studios éditable (modales modification) |
| **Web** | `PlanningGrid.razor`, `ReservationFormModal.razor` | Badge « Indisponible », filtrage dans le formulaire |
| **Tests** | 6+ fichiers | Adaptation `Studio.Create()`, nouveaux tests Unavailable |

## Tâches

### 1. Domain

- **`Studio.cs`** — Ajouter propriété `bool Unavailable { get; private set; }` (défaut `false`). Ajouter méthode `Update(string name, int capacity, bool hasKitchen, bool rentableAlone, bool unavailable)`. Mettre à jour `Create()` avec le paramètre `unavailable`.
- **`IStudioRepository.cs`** — Ajouter `Task UpdateAsync(Studio studio, CancellationToken ct = default)`.

### 2. Application — Commande `UpdateStudio`

- **`UpdateStudioCommand`** (`IRequireAdmin`) : `Guid StudioId`, `string Name`, `int Capacity`, `bool HasKitchen`, `bool RentableAlone`, `bool Unavailable`.
- **`UpdateStudioCommandHandler`** : charge le studio, appelle `studio.Update(...)`, persiste via `UpdateAsync`.
- **`UpdateStudioCommandValidator`** : Name requis/max 100, Capacity ≥ 1.

### 3. Application — Guard réservation

- **`CreateReservationCommandHandler`** : après le fetch du studio, vérifier `studio.Unavailable` → throw `InvalidOperationException`.
- **`UpdateReservationCommandHandler`** : idem si le studio change.

### 4. Application — Propagation DTO

- **`StudioDto`** : ajouter `bool Unavailable`.
- Mettre à jour le mapping dans : `GetStudiosQueryHandler`, `GetMonthlyPlanningQueryHandler`, `GetReservationDetailQueryHandler`.
- **`GetDailyOccupationQueryHandler`** et **`GetRangeOccupationQueryHandler`** : exclure les studios indisponibles des KPIs (capacité totale et taux d'occupation).

### 5. Infrastructure

- **`StudioConfiguration.cs`** : `builder.Property(s => s.Unavailable).IsRequired().HasDefaultValue(false)`.
- **`StudioRepository.cs`** : implémenter `UpdateAsync`.
- **`DbInitializer.cs`** : mettre à jour les appels `Studio.Create(...)` avec `unavailable: false`.
- **Migration EF Core** : nouvelle colonne `Unavailable` avec défaut `false`.

### 6. Web — Admin

- **`Admin.razor`** onglet Studios : remplacer le tableau lecture seule par un tableau avec bouton « Modifier » par ligne ouvrant une modale. Champs : Nom (input text), Capacité (input number), Cuisine (checkbox), Louable seul (checkbox), Indisponible (checkbox). Retirer le badge « Catalogue figé ».

### 7. Web — Planning

- **`PlanningGrid.razor`** : afficher un badge « Indisponible » sur les studios marqués `Unavailable`.
- **`ReservationFormModal.razor`** : filtrer les studios indisponibles dans le dropdown de sélection.

### 8. Tests

- Adapter tous les appels `Studio.Create()` existants (6+ fichiers) pour le nouveau paramètre.
- Ajouter tests : refus de réservation sur studio indisponible, commande `UpdateStudio`.

## Points d'attention

- Les réservations **existantes** sur un studio marqué indisponible restent visibles dans le planning (elles ne sont pas supprimées). Seule la **création** de nouvelles réservations est bloquée.
- Les KPIs d'occupation excluent les studios indisponibles pour refléter la réalité opérationnelle.
- Le `DisplayOrder` n'est pas modifiable via l'admin (ordre figé).

**Livrable** : Studios modifiables par l'admin, champ Indisponible verrouillant les réservations, indicateurs visuels dans le planning.
