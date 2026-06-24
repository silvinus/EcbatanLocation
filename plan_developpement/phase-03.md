# Phase 3 : Couche Application - Commands & Queries

**Objectif** : Tous les use cases implémentés via CQRS/MediatR.

## Commands

1. **`CreerReservationCommand`** → Crée une réservation en statut "Demande"
   - Validation : dates cohérentes, studio existe, capacité, pas de chevauchement
   - Handler : appelle le domain service, persiste via repository

2. **`ModifierReservationCommand`** → Modifie les infos d'une réservation existante
   - Mêmes validations + vérification que la réservation est modifiable

3. **`AccepterReservationCommand`** → Passe le statut Demande → Acceptée
   - Enregistre qui + quand

4. **`ConfirmerReservationCommand`** → Passe le statut Acceptée → Confirmée

5. **`SupprimerReservationCommand`** → Supprime une réservation (soft delete ou réel)

6. **`MettreAJourGrilleTarifaireCommand`** → Met à jour les tarifs pour une année

## Queries

1. **`GetPlanningMensuelQuery`** → Retourne le planning d'un mois donné
   - Paramètres : Année, Mois, filtres optionnels (studio, statut, propriétaire)
   - Retour : liste de studios avec leurs réservations pour chaque jour

2. **`GetReservationDetailQuery`** → Détail d'une réservation par ID

3. **`GetOccupationJourQuery`** → Places occupées / disponibles pour un jour donné

4. **`GetGrilleTarifaireQuery`** → Grille tarifaire d'une année

5. **`GetStudiosQuery`** → Liste des studios

6. **`GetProprietairesQuery`** → Liste des propriétaires

7. **`EstimerMontantQuery`** → Calcul estimatif du montant d'une réservation

## DTOs

- `PlanningMensuelDto`, `JourPlanningDto`, `ReservationPlanningDto`
- `ReservationDetailDto`
- `OccupationJourDto`
- `StudioDto`, `ProprietaireDto`
- `GrilleTarifaireDto`

## Validators (FluentValidation)

- Validation de chaque Command (dates, nombres positifs, enums valides)

**Livrable** : Tous les use cases fonctionnent en tests d'intégration.
