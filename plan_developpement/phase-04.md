# Phase 4 : Couche Infrastructure - Persistence

**Objectif** : Repositories EF Core, configurations, migrations.

## Tâches

1. **Configurations EF Core (Fluent API)**
   - `StudioConfiguration` : table Studios, propriétés, seed data
   - `ReservationConfiguration` : table Reservations, index sur Studio+Dates
   - `GrilleTarifaireConfiguration` : tables GrillesTarifaires + LignesTarifs
   - `ProprietaireConfiguration` : table Proprietaires, FK vers AspNetUsers

2. **Implémentation des Repositories**
   - `ReservationRepository` : CRUD + méthode `ExisteChevauche(studioId, dateRange, excludeId?)`
   - `StudioRepository` : lecture seule (catalogue figé)
   - `GrilleTarifaireRepository` : CRUD par année
   - `ProprietaireRepository` : lecture

3. **Migration initiale**
   - Génération de la migration EF Core
   - Vérification que la DB se crée correctement au démarrage

4. **Seed data**
   - Méthode `DbInitializer` appelée au démarrage
   - Crée les studios, propriétaires, comptes Identity, grille tarifaire 2026

5. **Tests Infrastructure**
   - Tests d'intégration avec SQLite in-memory
   - Vérification des contraintes (unicité, chevauchement)

**Livrable** : Persistence fonctionnelle, seed data, migrations.
