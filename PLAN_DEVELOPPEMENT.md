# Plan de développement - Ecbatan Location

## Vue d'ensemble

Le développement est découpé en **7 phases** progressives, chaque phase livrant un incrément fonctionnel testable.

---

## Phase 1 : Scaffolding & Infrastructure de base

**Objectif** : Solution compilable avec l'architecture DDD en place, base SQLite fonctionnelle, Identity configuré.

### Tâches

1. **Créer la structure de la solution**
   - Solution `EcbatanLocation.sln`
   - Projets : Domain, Application, Infrastructure, Web (Blazor Server)
   - Projets de tests : Domain.Tests, Application.Tests, Infrastructure.Tests
   - Références inter-projets (Domain ← Application ← Infrastructure ← Web)

2. **Configurer les dépendances NuGet**
   - Domain : aucune dépendance externe
   - Application : MediatR, FluentValidation
   - Infrastructure : EF Core + EF Core SQLite, ASP.NET Identity EF Core
   - Web : Blazor Server

3. **Configurer EF Core + SQLite**
   - `EcbatanLocationDbContext` dans Infrastructure
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

## Phase 9 : Durcissement (sécurité, intégrité, architecture, UI)

**Objectif** : Corriger les points d'amélioration identifiés lors de la revue de code. Chaque item ci-dessous porte une décision validée avec le client (✅ à faire / ⛔ pas d'action / ❓ non tranché).

### Sécurité & autorisations

1. ✅ **Autorisation par rôle (`Admin`)**
   - `AuthorizationBehavior` ne vérifie aujourd'hui que `IsAuthenticated`, jamais les rôles. La grille tarifaire doit être réservée aux admins.
   - Introduire un marqueur de rôle requis (ex. `IRequireRole` / `IRequireAdmin`) et faire échouer la commande si l'utilisateur n'a pas le rôle.
   - Page `Admin.razor` : passer de `[Authorize]` à `[Authorize(Roles="Admin")]`.
   - **Christophe est propriétaire ET admin** : son compte doit cumuler les rôles `Owner` + `Admin`.

2. ✅ **Comptes Admin seedés**
   - Donner le rôle `Admin` au compte Christophe (en plus de `Owner`).
   - Ajouter un nouvel utilisateur **Sylvain** avec le rôle `Admin` (administrateur technique, pas forcément propriétaire).
   - Masquer le bouton/lien « Administration » aux non-admins dans `MainLayout`.

3. ⛔ **Mots de passe par défaut** — pas d'action pour le moment (à revoir avant mise en production réelle).

4. ⛔ **Contrôle de propriété sur les réservations** — pas de contrôle pour le moment : tout propriétaire peut modifier/valider toute réservation (y compris la sienne). Décision assumée.

### Intégrité des données

5. ✅ **Race condition de double-réservation (TOCTOU)**
   - Encadrer la vérification de chevauchement + persistance dans **une transaction**.
   - Ajouter en complément un **index** (contrainte d'exclusion / index sur Studio + dates) garantissant l'absence de chevauchement au niveau base.

6. ⛔ **Tarif enfants -3 ans hors « Connaissance »** — comportement actuel conservé (plein tarif), pas d'action.

7. ⛔ **Réservation à cheval sur deux années** — non supporté volontairement : faire **deux réservations** distinctes. Pas de réservation à cheval sur l'année. À documenter dans l'UI/aide si besoin.

### Architecture

8. ✅ **Déplacer la logique de tarification dans le Domain**
   - Sortir le calcul de `EstimateAmountQueryHandler` vers le Domain (ex. `PricingGrid.CalculateAmount(personLines, numberOfDays)`).
   - Le handler ne fait plus que charger la grille et déléguer.

9. ✅ **Uniformiser les DTOs en anglais**
   - Renommer les DTOs francisés : `PlanningMensuelDto` → `MonthlyPlanningDto`, `OccupationJourDto` → `DailyOccupationDto`, `OccupationRangeDto` → `RangeOccupationDto` (et propager dans Queries/UI).
   - Cohérence totale avec la convention « tout le code en anglais ».

10. ✅ **Mettre en place des domain events**
    - Infrastructure de domain events (ex. `IDomainEvent`, collection d'events sur l'aggregate `Reservation`, dispatch après `SaveChanges`).
    - Premiers events : `ReservationCreated`, `ReservationAccepted`, `ReservationConfirmed`, `ReservationDeleted`.
    - Prépare les futures notifications / audit (nice-to-have ultérieurs).

11. ✅ **Tester la couche Application**
    - Le projet `EcbatanLocation.Application.Tests` ne contient aujourd'hui aucun test.
    - Couvrir les handlers (création, modification, transitions de statut, estimation multi-lignes) et les validators FluentValidation, avec repositories mockés.

### UI / UX

12. ✅ **Découper `Home.razor`**
    - Le composant fait ~400 lignes (état, chargement, sélection de plage, pilotage des modales).
    - Extraire l'état/orchestration (container ou service de page) et alléger le composant.

13. ✅ **Supprimer les styles inline**
    - Remplacer les `style="display:flex;gap:..."` dispersés (Home, Admin) par des classes CSS du thème.

14. ✅ **Accessibilité**
    - `aria-label` sur les boutons de navigation `<` / `>`.
    - Statuts non différenciés uniquement par la couleur : ajouter icône/texte (daltonisme).
    - Gestion du focus dans les modales (focus trap, fermeture clavier).

15. ⛔ **Transitions de statut « retour » (refuser/annuler/rétrograder)** — pas d'action pour le moment.

16. ❓ **Confidentialité des infos en mode public (H4)** — non tranché. À décider plus tard (RGPD / masquage nom locataire pour les anonymes).

### Nice-to-have (à rediscuter plus tard)

- Export iCal / abonnement calendrier
- Notifications email
- Historique/audit complet des modifications
- Récapitulatif annuel par propriétaire (nuitées, montants)
- Versionner le dossier `deployement/`

**Livrable** : Application durcie côté sécurité (rôles admin), intégrité (anti double-réservation), architecture (tarif au Domain, DTOs anglais, domain events, tests Application) et UI (découpage, styles, accessibilité).

### Statut d'avancement (Phase 9 livrée)

| # | Item | Statut |
|---|------|--------|
| 1 | Autorisation par rôle Admin | ✅ Fait |
| 2 | Comptes Christophe (Owner+Admin) + Sylvain (Admin) | ✅ Fait |
| 3 | Mots de passe par défaut | ⛔ Sans action (à revoir avant prod) |
| 4 | Contrôle de propriété des réservations | ⛔ Sans action |
| 5 | Anti double-réservation (transaction + index unique) | ✅ Fait |
| 6 | Tarif enfants -3 ans hors « Connaissance » | ⛔ Sans action |
| 7 | Réservation à cheval sur deux années | ⛔ Sans action (faire 2 réservations) |
| 8 | Tarification déplacée dans le Domain (`PricingGrid.CalculateAmount`) | ✅ Fait |
| 9 | DTOs renommés en anglais (`MonthlyPlanningDto`, `DailyOccupationDto`, `RangeOccupationDto`) | ✅ Fait |
| 10 | Domain events (INotification + intercepteur EF) | ✅ Fait |
| 11 | Tests de la couche Application (26 tests) | ✅ Fait |
| 12 | Découpage `Home.razor` (code-behind) | ✅ Fait |
| 13 | Styles inline → classes CSS | ✅ Fait |
| 14 | Accessibilité (ARIA, clavier, focus, `role=dialog`) | ✅ Fait |
| 15 | Transitions de statut « retour » | ⛔ Sans action |
| 16 | Confidentialité publique (H4) | ❓ Non tranché |

**Tests** : 94 tests verts (Domain 44, Application 26, Infrastructure 24). Build sans warning.

**Vérification E2E** : parcours navigateur complet (Claude Preview) de tous les cas d'usage — public, login propriétaire/admin, création/édition/accepter/confirmer/supprimer, overlap, grille tarifaire, responsive mobile.

**Correctifs issus des tests E2E** :
- Message de succès admin « Grille tarifaire enregistrée » qui ne s'affichait jamais → corrigé.
- Accessibilité clavier de la vue Mois (`.booking-span`) qui manquait → ajoutée.
- Page `/Account/AccessDenied` conviviale ajoutée (au lieu d'un écran blanc).

**Commits** : `d27ed9e` (sécurité/intégrité/archi), `7328895` (domain events), `9f8e86b` (UI), `8c18e60` (correctifs E2E).

**Reste ouvert** : item 16 (confidentialité H4) à trancher ; nice-to-have à planifier.

---

## Phase 10 : Bugfix — Règle « studio non louable seul » (H1)

**Objectif** : Corriger la validation des studios non louables seuls. Actuellement un simple chevauchement partiel suffit ; la règle métier exige que la période du studio dépendant soit **entièrement incluse** dans celle de la réservation principale.

### Problème

`ReservationDomainService.ValidateStudioDependency()` utilise `DateRange.Overlaps()` pour valider la dépendance. Un propriétaire peut donc réserver un studio dépendant (ex : Studio Centre) du 3 au 12 juillet alors que sa réservation principale (ex : Villa) ne couvre que le 5 au 10 — les jours 3-4 et 10-12 débordent sans être couverts.

### Tâches

1. **`DateRange.cs`** — Ajouter une méthode `Contains(DateRange other)` : `StartDate <= other.StartDate && EndDate >= other.EndDate`

2. **`ReservationDomainService.cs`** — Dans `ValidateStudioDependency()`, remplacer `r.Dates.Overlaps(dates)` par `r.Dates.Contains(dates)` (la réservation principale doit englober la dépendante)

3. **`ReservationDomainServiceTests.cs`** — Corriger le test existant `WithOverlappingReservation_Passes` (la réservation principale doit englober la dépendante). Ajouter les cas :
   - Réservation dépendante qui déborde au début → doit échouer
   - Réservation dépendante qui déborde à la fin → doit échouer
   - Réservation dépendante qui déborde des deux côtés → doit échouer
   - Réservation dépendante strictement incluse → doit passer

4. **Tests `DateRange`** — Ajouter des tests unitaires pour `Contains(DateRange)` (inclusion stricte, identique, débordement partiel, aucun chevauchement)

### Approche

Option A retenue : la requête du repository (`GetByOwnerAndOverlappingDatesAsync`) reste inchangée (filtre large par chevauchement). Le filtrage strict par inclusion est fait dans le domain service, ce qui garde le repo réutilisable.

**Livrable** : Correction du bug H1, tests verts couvrant tous les cas limites.

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
| **9** | **Durcissement (sécurité, intégrité, archi, UI)** | **✅ Livrée** |
| **10** | **Bugfix — Règle « studio non louable seul » (H1)** | **À faire** |

## Dépendances NuGet prévues

| Package | Projet | Usage |
|---------|--------|-------|
| FluentValidation | Application | Validation commands |
| FluentValidation.DependencyInjectionExtensions | Application | Auto-registration |
| Microsoft.Extensions.DependencyInjection.Abstractions | Application | DI (médiateur maison) |
| Microsoft.Extensions.Logging.Abstractions | Application | Logging (handlers d'events) |
| Microsoft.EntityFrameworkCore | Infrastructure | ORM |
| Microsoft.EntityFrameworkCore.Sqlite | Infrastructure | Provider SQLite |
| Microsoft.EntityFrameworkCore.Tools | Infrastructure | Migrations |
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | Infrastructure | Identity |
| Microsoft.AspNetCore.Components.Authorization | Web | Auth Blazor |

> **CQRS** : assuré par un **médiateur maison** (`EcbatanLocation.Application/Messaging`), sans dépendance externe. MediatR a été retiré (passé sous licence commerciale en v13+) au profit d'une solution 100 % open source.
