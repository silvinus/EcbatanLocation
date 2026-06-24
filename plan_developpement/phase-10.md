# Phase 10 : Bugfix — Règle « studio non louable seul » (H1)

**Objectif** : Corriger la validation des studios non louables seuls. Actuellement un simple chevauchement partiel suffit ; la règle métier exige que la période du studio dépendant soit **entièrement incluse** dans celle de la réservation principale.

## Problème

`ReservationDomainService.ValidateStudioDependency()` utilise `DateRange.Overlaps()` pour valider la dépendance. Un propriétaire peut donc réserver un studio dépendant (ex : Studio Centre) du 3 au 12 juillet alors que sa réservation principale (ex : Villa) ne couvre que le 5 au 10 — les jours 3-4 et 10-12 débordent sans être couverts.

## Tâches

1. **`DateRange.cs`** — Ajouter une méthode `Contains(DateRange other)` : `StartDate <= other.StartDate && EndDate >= other.EndDate`

2. **`ReservationDomainService.cs`** — Dans `ValidateStudioDependency()`, remplacer `r.Dates.Overlaps(dates)` par `r.Dates.Contains(dates)` (la réservation principale doit englober la dépendante)

3. **`ReservationDomainServiceTests.cs`** — Corriger le test existant `WithOverlappingReservation_Passes` (la réservation principale doit englober la dépendante). Ajouter les cas :
   - Réservation dépendante qui déborde au début → doit échouer
   - Réservation dépendante qui déborde à la fin → doit échouer
   - Réservation dépendante qui déborde des deux côtés → doit échouer
   - Réservation dépendante strictement incluse → doit passer

4. **Tests `DateRange`** — Ajouter des tests unitaires pour `Contains(DateRange)` (inclusion stricte, identique, débordement partiel, aucun chevauchement)

## Approche

Option A retenue : la requête du repository (`GetByOwnerAndOverlappingDatesAsync`) reste inchangée (filtre large par chevauchement). Le filtrage strict par inclusion est fait dans le domain service, ce qui garde le repo réutilisable.

**Livrable** : Correction du bug H1, tests verts couvrant tous les cas limites.
