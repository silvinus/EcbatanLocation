# Phase 8 : Multi-typologies de personnes par réservation

**Objectif** : Permettre de mixer plusieurs types de clients dans une même réservation (ex : 2 propriétaires + 2 invités + 2 enfants connaissance) pour un calcul de tarif précis.

## Règles métier

- Une réservation contient **une ou plusieurs lignes de personnes** (`PersonLine`), chacune avec : `ClientType`, `AdultCount`, `ChildrenUnder3Count`
- **MobileHome/Tente sont exclusifs** : si le studio est un Mobil-home → type forcé `MobileHome`, idem Tente → `Tent`. Une seule ligne, pas de mix.
- Pour les autres studios : mix libre entre `Owner`, `GuestWithPresence`, `Acquaintance`
- Chaque ligne a ses propres adultes + enfants, le tarif enfant dépend du type de la ligne
- Capacité totale (somme de toutes les lignes) ≤ capacité du studio

## Tâches

1. **Domain**
   - Créer value object `PersonLine` (`ClientType`, `AdultCount`, `ChildrenUnder3Count`)
   - Modifier `Reservation` : remplacer `AdultCount`, `ChildrenUnder3Count`, `ClientType` par une collection `PersonLines`
   - Adapter validations (capacité, au moins 1 adulte au total)
   - Propriétés calculées : `TotalPersonCount`, `TotalAdultCount`, `TotalChildrenUnder3Count`

2. **Infrastructure**
   - Mapper `PersonLine` comme owned entity collection (`OwnsMany`) → table `ReservationPersonLines`
   - Migration EF Core avec migration des données existantes

3. **Application**
   - Créer `PersonLineDto`
   - Modifier `CreateReservationCommand` / `UpdateReservationCommand` : remplacer les 3 champs par `IReadOnlyList<PersonLineDto>`
   - Modifier `EstimateAmountQuery` + Handler : itérer sur chaque ligne, tarif par type
   - Modifier `ReservationDetailDto` / `ReservationPlanningDto`
   - Adapter validators FluentValidation
   - Adapter `GetReservationDetailQueryHandler`

4. **UI Blazor**
   - `ReservationFormModal.razor` : système de lignes dynamiques (ajouter/supprimer des types)
     - Si studio MobileHome/Tente : une seule ligne, type forcé, non modifiable
     - Sinon : liste de lignes avec bouton "+ Ajouter un type" et "×" pour supprimer
   - `ReservationDetailModal.razor` : affichage détaillé par ligne de personnes
   - `ReservationCell.razor` : affichage compact avec total personnes
   - CSS : styles pour `.person-lines`, `.person-line-row`, boutons ajouter/supprimer

5. **Tests**
   - Domain : `PersonLine` validation, `Reservation.Create/Update` multi-lignes, capacité
   - Application : `EstimateAmountQueryHandler` multi-lignes, validators
   - Adapter tests existants

**Livrable** : Tarification précise par typologie de personne dans chaque réservation.
