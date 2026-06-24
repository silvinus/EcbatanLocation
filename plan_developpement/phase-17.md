# Phase 17 : Capacité — Exclure les enfants de moins de 3 ans

**Objectif** : Les enfants de moins de 3 ans ne comptent plus dans la vérification de capacité d'un studio. Seuls les adultes sont comparés à la capacité maximale du logement.

## Règle métier modifiée

- **Avant** : `Adultes + Enfants -3 ans ≤ Capacité du studio`
- **Après** : `Adultes ≤ Capacité du studio`

Les enfants de moins de 3 ans restent enregistrés (pour la tarification et l'affichage) mais ne bloquent plus la réservation quand leur présence fait dépasser la capacité.

## Analyse d'impact

| Couche | Fichier | Nature de l'impact |
|--------|---------|-------------------|
| **Domain** | `Reservation.cs` | `ValidatePersonLines()` : remplacer `totalPersons` par `totalAdults` dans la comparaison à `studioCapacity` |
| **Tests** | `ReservationTests.cs` | 4 tests à adapter : `Create_CapacityExceeded_Throws`, `Create_ExactCapacity_Succeeds`, `Create_MultipleLines_CapacityExceeded_Throws`, `Update_CapacityExceeded_Throws` — les valeurs de test doivent provoquer le dépassement par les seuls adultes |
| **Doc** | `CLAUDE.md` | Mettre à jour la règle invariante de capacité |

## Tâches

1. **`Reservation.cs`** — Dans `ValidatePersonLines()`, remplacer :
   ```csharp
   var totalPersons = lines.Sum(l => l.TotalPersons);
   if (totalPersons > studioCapacity)
   ```
   par :
   ```csharp
   if (totalAdults > studioCapacity)
   ```
   La variable `totalAdults` est déjà calculée ligne 120 pour la validation « au moins 1 adulte ».

2. **`ReservationTests.cs`** — Ajuster les 4 tests pour que le dépassement repose uniquement sur le nombre d'adultes (ex : `adultCount: 7, capacity: 6` au lieu de `adultCount: 4, childrenCount: 3`). Ajouter un test vérifiant qu'une réservation avec beaucoup d'enfants mais peu d'adultes passe la validation.

3. **`CLAUDE.md`** — Mettre à jour la règle « Capacité : Adultes + Enfants ≤ Capacité du studio » en « Capacité : Adultes ≤ Capacité du studio (les enfants -3 ans ne comptent pas) ».

## Ce qui ne change PAS

- `PersonLine.TotalPersons` (adultes + enfants) : conservé pour l'affichage et la tarification
- `Reservation.TotalPersonCount` : idem, compteur d'affichage
- Tarification : les enfants -3 ans restent facturés selon la grille tarifaire
- KPIs d'occupation : basés sur la capacité du studio, pas le nombre de personnes

**Livrable** : Validation de capacité basée uniquement sur les adultes, tests adaptés.
