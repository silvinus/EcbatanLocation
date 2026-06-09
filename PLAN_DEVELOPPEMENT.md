# Plan de développement - Planning Location

## Vue d'ensemble

Le développement est découpé en **7 phases** progressives, chaque phase livrant un incrément fonctionnel testable.

---

## Phase 1 : Scaffolding & Infrastructure de base

**Objectif** : Solution compilable avec l'architecture DDD en place, base SQLite fonctionnelle, Identity configuré.

### Tâches

1. **Créer la structure de la solution**
   - Solution `PlanningLocation.sln`
   - Projets : Domain, Application, Infrastructure, Web (Blazor Server)
   - Projets de tests : Domain.Tests, Application.Tests, Infrastructure.Tests
   - Références inter-projets (Domain ← Application ← Infrastructure ← Web)

2. **Configurer les dépendances NuGet**
   - Domain : aucune dépendance externe
   - Application : MediatR, FluentValidation
   - Infrastructure : EF Core + EF Core SQLite, ASP.NET Identity EF Core
   - Web : Blazor Server

3. **Configurer EF Core + SQLite**
   - `PlanningLocationDbContext` dans Infrastructure
   - Connection string dans `appsettings.json`
   - Configuration Identity (IdentityUser étendu pour les propriétaires)

4. **Configurer ASP.NET Identity**
   - Classe `ApplicationUser` (IdentityUser étendu avec Nom, IsProprietaire)
   - Rôles : `Public`, `Proprietaire`, `Admin`
   - Pages de login/logout Blazor (basiques)

5. **Seed des données initiales**
   - 7 studios (catalogue figé)
   - 4 comptes propriétaires
   - Grille tarifaire 2026
   - Rôles

6. **Pipeline MediatR**
   - Configuration dans `Program.cs`
   - Behavior de validation (FluentValidation)
   - Behavior de logging (optionnel)

**Livrable** : L'app démarre, on peut se connecter, la DB est seedée.

---

## Phase 2 : Couche Domain - Modèle métier

**Objectif** : Entités riches avec logique métier, value objects, interfaces de repository.

### Tâches

1. **Entités et Value Objects**
   - `Studio` (entité) : Id, Nom, Capacite, ACuisine, LouableSeul
   - `Proprietaire` (entité) : Id, Nom, UserId (lien Identity)
   - `Reservation` (aggregate root) : tous les champs du CDC
   - `DateRange` (value object) : DateDebut, DateFin, avec validation et méthode `Chevauche(DateRange other)`
   - `TypeClient` (enum) : Proprietaire, InviteAvecPresence, Connaissance, MobilHome, Tente
   - `StatutReservation` (enum) : Demande, Acceptee, Confirmee
   - `GrilleTarifaire` (entité) : Annee, liste de `LigneTarif`
   - `LigneTarif` (value object) : TypeClient, PrixParJourParPersonne

2. **Règles métier dans Reservation**
   - Méthode `Accepter(string parQui, DateTime quand)` : transition Demande → Acceptée
   - Méthode `Confirmer(string parQui, DateTime quand)` : transition Acceptée → Confirmée
   - Validation capacité dans le constructeur
   - Propriétés de traçabilité (AccepteePar, AccepteeLe)

3. **Interfaces de repository**
   - `IReservationRepository`
   - `IStudioRepository`
   - `IGrilleTarifaireRepository`
   - `IProprietaireRepository`

4. **Domain Services**
   - `ReservationDomainService` : vérification chevauchement, vérification règle "non louable seul"

5. **Tests unitaires Domain**
   - Tests des invariants de Reservation (chevauchement, capacité, transitions de statut)
   - Tests du DateRange (chevauchement, validité)
   - Tests du ReservationDomainService

**Livrable** : Modèle métier solide et testé, aucune dépendance infrastructure.

---

## Phase 3 : Couche Application - Commands & Queries

**Objectif** : Tous les use cases implémentés via CQRS/MediatR.

### Commands

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

### Queries

1. **`GetPlanningMensuelQuery`** → Retourne le planning d'un mois donné
   - Paramètres : Année, Mois, filtres optionnels (studio, statut, propriétaire)
   - Retour : liste de studios avec leurs réservations pour chaque jour

2. **`GetReservationDetailQuery`** → Détail d'une réservation par ID

3. **`GetOccupationJourQuery`** → Places occupées / disponibles pour un jour donné

4. **`GetGrilleTarifaireQuery`** → Grille tarifaire d'une année

5. **`GetStudiosQuery`** → Liste des studios

6. **`GetProprietairesQuery`** → Liste des propriétaires

7. **`EstimerMontantQuery`** → Calcul estimatif du montant d'une réservation

### DTOs

- `PlanningMensuelDto`, `JourPlanningDto`, `ReservationPlanningDto`
- `ReservationDetailDto`
- `OccupationJourDto`
- `StudioDto`, `ProprietaireDto`
- `GrilleTarifaireDto`

### Validators (FluentValidation)

- Validation de chaque Command (dates, nombres positifs, enums valides)

**Livrable** : Tous les use cases fonctionnent en tests d'intégration.

---

## Phase 4 : Couche Infrastructure - Persistence

**Objectif** : Repositories EF Core, configurations, migrations.

### Tâches

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

---

## Phase 5 : UI Blazor - Planning & Lecture publique

**Objectif** : L'écran principal du planning est fonctionnel et consultable publiquement.

### Tâches

1. **Layout principal**
   - `MainLayout.razor` : thème sombre (CSS basé sur les maquettes)
   - Barre de navigation : titre, bouton connexion, bouton nouvelle réservation
   - Layout responsive (grid 340px sidebar + 1fr main)

2. **Page Planning (`/`)**
   - Composant `PlanningMensuel.razor` : tableau studios × jours
   - Colonne sticky pour les noms de studios
   - Cellules avec réservations colorées par statut
   - Navigation mois précédent / suivant

3. **Sidebar Filtres & Synthèse**
   - Composant `FiltresSynthese.razor`
   - Filtres : mois, studio, statut, propriétaire
   - KPIs : places occupées/disponibles, studios occupés (pour le jour survolé ou sélectionné)
   - Légende des statuts

4. **Composant Réservation (cellule)**
   - `ReservationCell.razor` : affiche nom locataire, propriétaire, badge statut, nb personnes
   - Couleur de bordure selon statut
   - Clic → détail (modal ou panel)

5. **Modal Détail Réservation**
   - Lecture seule en mode public
   - Affiche toutes les infos + estimation montant

6. **CSS / Thème**
   - Variables CSS issues des maquettes (--bg, --panel, --brand, --ok, --warn, --danger)
   - Composants visuels : cards, chips, badges, boutons
   - Responsive breakpoint à 1020px

**Livrable** : Planning consultable publiquement, filtres, KPIs, thème sombre fidèle aux maquettes.

---

## Phase 6 : UI Blazor - Authentification & Édition propriétaire

**Objectif** : Les propriétaires peuvent se connecter et gérer les réservations.

### Tâches

1. **Pages d'authentification**
   - Page Login (`/login`) : formulaire email + mot de passe
   - Logout
   - Gestion de session Blazor Server (AuthenticationStateProvider)

2. **Adaptation du planning en mode connecté**
   - Bouton "+ Nouvelle réservation" visible uniquement pour les propriétaires
   - Actions sur les réservations existantes (modifier, changer statut)
   - Indicateur visuel du mode connecté

3. **Modal Création / Modification de réservation**
   - Formulaire complet : studio, dates, propriétaire, locataire, nb adultes, nb enfants, type client
   - Validation côté client (FluentValidation + affichage erreurs)
   - Contrôle chevauchement en temps réel (appel query)
   - Estimation du montant en temps réel
   - Boutons : Enregistrer / Valider

4. **Actions de changement de statut**
   - Boutons contextuels : "Accepter" (si Demande), "Confirmer" (si Acceptée)
   - Confirmation avant action
   - Traçabilité automatique (qui + quand)

5. **Gestion des autorisations Blazor**
   - `AuthorizeView` pour masquer les actions d'édition
   - Vérification côté serveur dans les handlers (double sécurité)

**Livrable** : Circuit complet de gestion des réservations par les propriétaires.

---

## Phase 7 : Finalisation & Déploiement

**Objectif** : Application prête pour la production sur VPS.

### Tâches

1. **Administration (optionnel)**
   - Page gestion des tarifs (édition grille annuelle)
   - Page gestion des studios (si besoin de modifier le catalogue)

2. **Polish UI**
   - Vues additionnelles : Semaine, Liste
   - Animations / transitions
   - Messages de confirmation / erreur
   - Loading states

3. **Sécurité**
   - HTTPS forcé
   - Anti-forgery tokens
   - Rate limiting basique
   - Headers de sécurité (CSP, HSTS)

4. **Configuration déploiement**
   - `Dockerfile` (optionnel) ou publication standalone
   - Configuration `appsettings.Production.json`
   - Script de déploiement (systemd service sur Linux)
   - Backup automatique SQLite (cron)

5. **Tests end-to-end**
   - Scénario complet : consultation publique → login → créer réservation → accepter → confirmer
   - Test responsive
   - Test des règles métier depuis l'UI

6. **Documentation**
   - Guide de déploiement
   - Guide utilisateur basique

**Livrable** : Application en production sur le VPS.

---

## Phase 8 : Multi-typologies de personnes par réservation

**Objectif** : Permettre de mixer plusieurs types de clients dans une même réservation (ex : 2 propriétaires + 2 invités + 2 enfants connaissance) pour un calcul de tarif précis.

### Règles métier

- Une réservation contient **une ou plusieurs lignes de personnes** (`PersonLine`), chacune avec : `ClientType`, `AdultCount`, `ChildrenUnder3Count`
- **MobileHome/Tente sont exclusifs** : si le studio est un Mobil-home → type forcé `MobileHome`, idem Tente → `Tent`. Une seule ligne, pas de mix.
- Pour les autres studios : mix libre entre `Owner`, `GuestWithPresence`, `Acquaintance`
- Chaque ligne a ses propres adultes + enfants, le tarif enfant dépend du type de la ligne
- Capacité totale (somme de toutes les lignes) ≤ capacité du studio

### Tâches

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

---

## Résumé des phases

| Phase | Contenu | Estimation |
|-------|---------|-----------|
| 1 | Scaffolding & Infrastructure | Fondations |
| 2 | Domain (modèle métier) | Coeur métier |
| 3 | Application (CQRS) | Use cases |
| 4 | Infrastructure (persistence) | Base de données |
| 5 | UI Planning public | Écran principal |
| 6 | UI Authentification & Édition | Gestion réservations |
| 7 | Finalisation & Déploiement | Production |
| **8** | **Multi-typologies personnes** | **Tarification précise** |

## Dépendances NuGet prévues

| Package | Projet | Usage |
|---------|--------|-------|
| MediatR | Application | CQRS |
| FluentValidation | Application | Validation commands |
| FluentValidation.DependencyInjectionExtensions | Application | Auto-registration |
| Microsoft.EntityFrameworkCore | Infrastructure | ORM |
| Microsoft.EntityFrameworkCore.Sqlite | Infrastructure | Provider SQLite |
| Microsoft.EntityFrameworkCore.Tools | Infrastructure | Migrations |
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | Infrastructure | Identity |
| Microsoft.AspNetCore.Components.Authorization | Web | Auth Blazor |
