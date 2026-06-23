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

## Phase 11 : Gestion des utilisateurs (Admin)

**Objectif** : Permettre à un administrateur de gérer les comptes utilisateurs (propriétaires et administrateurs) depuis l'interface d'administration : ajouter, modifier, supprimer, et régénérer un mot de passe aléatoire.

### Analyse d'impact

| Couche | Fichiers impactés | Nature de l'impact |
|--------|-------------------|--------------------|
| **Domain** | `Owner.cs`, `IOwnerRepository.cs` | `Owner.Update()` pour renommer. Ajout de `AddAsync`, `UpdateAsync`, `DeleteAsync` au repository. |
| **Application** | Nouveaux dossiers Commands + Queries | 5 nouvelles opérations CQRS (voir ci-dessous). Nouveau DTO `UserDto`. |
| **Infrastructure** | `OwnerRepository.cs`, `DbInitializer.cs` | Implémentation des nouvelles méthodes du repository. Interaction `UserManager` pour Identity. |
| **Web** | `Admin.razor` | Nouvel onglet « Utilisateurs » dans la page admin existante. |
| **Tests** | Domain.Tests, Application.Tests | Tests unitaires des nouvelles rules + handlers. |

### Règles métier & contraintes

- Un utilisateur peut avoir le rôle **Owner**, **Admin**, ou les deux (comme Christophe).
- Un **Owner** a une entrée dans la table `Owners` (liée à `AspNetUsers` via `UserId`). Un Admin pur n'en a pas.
- **Suppression** : un propriétaire ne peut être supprimé que s'il n'a **aucune réservation** existante (intégrité référentielle). Si des réservations existent, l'admin doit d'abord les réaffecter ou supprimer.
- **Mot de passe régénéré** : mot de passe aléatoire de 12 caractères (majuscules, minuscules, chiffres, caractère spécial). Affiché **une seule fois** à l'admin dans une modale de confirmation (pas d'envoi email — pas de serveur SMTP configuré).
- L'admin ne peut pas se supprimer lui-même.
- Il doit toujours rester au moins un compte Admin.

### Tâches

#### 1. Domain

- **`Owner.cs`** — Ajouter méthode `Update(string name)` pour permettre le renommage.
- **`IOwnerRepository.cs`** — Ajouter : `Task AddAsync(Owner owner, CancellationToken ct)`, `Task UpdateAsync(Owner owner, CancellationToken ct)`, `Task DeleteAsync(Owner owner, CancellationToken ct)`.

#### 2. Application — Commands

1. **`CreateUserCommand`** (`IRequireAdmin`)
   - Input : `DisplayName`, `Email`, `Roles[]` (Owner/Admin)
   - Handler : crée `ApplicationUser` via `UserManager`, assigne les rôles, crée l'entrée `Owner` si rôle Owner, génère un mot de passe aléatoire.
   - Retour : `CreatedUserResult` contenant le mot de passe généré (affiché une fois).

2. **`UpdateUserCommand`** (`IRequireAdmin`)
   - Input : `UserId`, `DisplayName`, `Email`, `Roles[]`
   - Handler : met à jour `ApplicationUser`, ajuste rôles, met à jour ou crée/supprime l'entrée `Owner` selon le changement de rôle.

3. **`DeleteUserCommand`** (`IRequireAdmin`)
   - Input : `UserId`
   - Handler : vérifie aucune réservation liée, vérifie que ce n'est pas le dernier admin, supprime `Owner` + `ApplicationUser`.

4. **`ResetPasswordCommand`** (`IRequireAdmin`)
   - Input : `UserId`
   - Handler : génère un mot de passe aléatoire via `UserManager.ResetPasswordAsync`, retourne le nouveau mot de passe.

#### 3. Application — Queries

5. **`GetUsersQuery`** (`IRequireAdmin`)
   - Retour : `IReadOnlyList<UserDto>` — Id, DisplayName, Email, Roles[], IsOwner, HasReservations (pour savoir si supprimable).

#### 4. Application — DTOs & Validators

- **`UserDto`** : `UserId`, `DisplayName`, `Email`, `Roles`, `IsOwner`, `HasReservations`
- **`CreatedUserResult`** : `UserId`, `GeneratedPassword`
- **Validators** : email valide et unique, nom non vide, au moins un rôle, mot de passe conforme aux règles Identity.

#### 5. Infrastructure

- **`OwnerRepository.cs`** — Implémenter `AddAsync`, `UpdateAsync`, `DeleteAsync`.
- **Handlers** — Les handlers `CreateUser`, `UpdateUser`, `DeleteUser`, `ResetPassword` utilisent directement `UserManager<ApplicationUser>` (injecté). Pas besoin d'abstraction supplémentaire — c'est de la logique d'infrastructure/Identity, pas du domaine pur.

#### 6. Web — UI

- **`Admin.razor`** — Ajouter un 3e onglet « Utilisateurs » dans les tabs existants :
  - Tableau listant tous les utilisateurs : nom, email, rôles (badges), actions.
  - Bouton « + Nouvel utilisateur » ouvrant une modale de création.
  - Actions par ligne : Modifier (modale), Régénérer mot de passe (confirmation + affichage), Supprimer (confirmation).
  - Modale création/modification : champs DisplayName, Email, checkboxes Propriétaire/Administrateur.
  - Modale mot de passe généré : affiche le mot de passe en clair une seule fois avec bouton copier.
  - Le bouton supprimer est désactivé si l'utilisateur a des réservations (tooltip explicatif).

#### 7. Tests

- **Domain.Tests** : `Owner.Update()` renomme correctement.
- **Application.Tests** :
  - `CreateUserCommandHandler` : crée user + owner si rôle Owner, refuse email dupliqué.
  - `DeleteUserCommandHandler` : refuse si réservations existantes, refuse si dernier admin.
  - `ResetPasswordCommandHandler` : retourne un mot de passe valide.
  - `GetUsersQueryHandler` : retourne la liste avec rôles et flag `HasReservations`.

### Points d'attention

- **Pas de migration EF Core nécessaire** : `Owner` et `AspNetUsers` existent déjà. Les nouvelles opérations sont des CRUD sur les tables existantes.
- **`UserManager`** gère déjà la complexité Identity (hashing, validation mot de passe, rôles). On s'appuie dessus sans réinventer.
- **Accès** : toutes les opérations marquées `IRequireAdmin` → sécurisées via `AdminAuthorizationBehavior` existant.

**Livrable** : Page de gestion des utilisateurs dans l'admin, CRUD complet propriétaires/admins, régénération de mot de passe aléatoire.

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
| **11** | **Gestion des utilisateurs (Admin)** | **À faire** |
| **12** | **Types client flexibles Mobil-home / Tente** | **✅ Livrée** |
| **13** | **Gestion admin des studios + champ Indisponible** | **✅ Livrée** |
| **14** | **Rapport de réservations avec calcul de prix + export PDF** | **À faire** |
| **15** | **Migration déploiement Fly.io → Northflank** | **À faire** |
| **16** | **Support double base de données SQLite / PostgreSQL** | **✅ Livrée** |
| **17** | **Capacité — Exclure les enfants -3 ans** | **À faire** |
| **18** | **Durcissement authentification** | **À faire** |

## Phase 12 : Types client flexibles Mobil-home / Tente

**Objectif** : Permettre de choisir le type de client pour les studios Mobil-home et Tente, au lieu de forcer un type exclusif.

### Problème

Actuellement, sélectionner un studio Mobil-home force le type `MobileHome` et sélectionner un emplacement Tente force le type `Tent`. Le dropdown est désactivé, une seule ligne de personnes est autorisée. Cela empêche de facturer au tarif propriétaire ou invité quand c'est pertinent.

### Nouvelle règle

| Studio | Types autorisés |
|--------|----------------|
| Mobil-home | MobileHome, Owner, GuestWithPresence |
| Emplacement tente | Tent, Owner, GuestWithPresence |
| Autres studios | Owner, GuestWithPresence, Acquaintance |

- Les studios Mobil-home et Tente permettent désormais **plusieurs lignes de personnes** et le **mix de types**.
- Le dropdown est actif (plus désactivé).
- Les types `Acquaintance` (pour Mobil-home/Tente) et `MobileHome`/`Tent` (pour les studios classiques) restent interdits.

### Tâches

1. **`ReservationFormModal.razor`** — Refondre `UpdateExclusiveType()` :
   - Remplacer le flag booléen `_isExclusiveType` par une liste `_allowedClientTypes` calculée selon le studio.
   - Supprimer le forçage du type et la limitation à 1 ligne.
   - Filtrer le dropdown des types selon `_allowedClientTypes`.
   - Adapter `AddLine()` pour choisir le prochain type parmi les types autorisés.
   - Réactiver le bouton "+" et le bouton "×" pour Mobil-home/Tente.

### Impact

| Couche | Impact |
|--------|--------|
| Domain | Aucun |
| Application (validators) | Aucun (validation `.IsInEnum()` suffisante) |
| Infrastructure | Aucun |
| Tarification | Aucun (grille de prix existante couvre tous les types) |
| UI | Seul `ReservationFormModal.razor` est modifié |

**Livrable** : Types de client flexibles pour Mobil-home et Tente, multi-lignes autorisées.

---

## Phase 13 : Gestion admin des studios + champ Indisponible

**Objectif** : Rendre le catalogue de studios modifiable par un administrateur (nom, capacité, cuisine, louable seul) et ajouter un champ **Indisponible** (`Unavailable`) permettant de verrouiller un logement pour empêcher toute nouvelle réservation.

### Analyse d'impact

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

### Tâches

#### 1. Domain

- **`Studio.cs`** — Ajouter propriété `bool Unavailable { get; private set; }` (défaut `false`). Ajouter méthode `Update(string name, int capacity, bool hasKitchen, bool rentableAlone, bool unavailable)`. Mettre à jour `Create()` avec le paramètre `unavailable`.
- **`IStudioRepository.cs`** — Ajouter `Task UpdateAsync(Studio studio, CancellationToken ct = default)`.

#### 2. Application — Commande `UpdateStudio`

- **`UpdateStudioCommand`** (`IRequireAdmin`) : `Guid StudioId`, `string Name`, `int Capacity`, `bool HasKitchen`, `bool RentableAlone`, `bool Unavailable`.
- **`UpdateStudioCommandHandler`** : charge le studio, appelle `studio.Update(...)`, persiste via `UpdateAsync`.
- **`UpdateStudioCommandValidator`** : Name requis/max 100, Capacity ≥ 1.

#### 3. Application — Guard réservation

- **`CreateReservationCommandHandler`** : après le fetch du studio, vérifier `studio.Unavailable` → throw `InvalidOperationException`.
- **`UpdateReservationCommandHandler`** : idem si le studio change.

#### 4. Application — Propagation DTO

- **`StudioDto`** : ajouter `bool Unavailable`.
- Mettre à jour le mapping dans : `GetStudiosQueryHandler`, `GetMonthlyPlanningQueryHandler`, `GetReservationDetailQueryHandler`.
- **`GetDailyOccupationQueryHandler`** et **`GetRangeOccupationQueryHandler`** : exclure les studios indisponibles des KPIs (capacité totale et taux d'occupation).

#### 5. Infrastructure

- **`StudioConfiguration.cs`** : `builder.Property(s => s.Unavailable).IsRequired().HasDefaultValue(false)`.
- **`StudioRepository.cs`** : implémenter `UpdateAsync`.
- **`DbInitializer.cs`** : mettre à jour les appels `Studio.Create(...)` avec `unavailable: false`.
- **Migration EF Core** : nouvelle colonne `Unavailable` avec défaut `false`.

#### 6. Web — Admin

- **`Admin.razor`** onglet Studios : remplacer le tableau lecture seule par un tableau avec bouton « Modifier » par ligne ouvrant une modale. Champs : Nom (input text), Capacité (input number), Cuisine (checkbox), Louable seul (checkbox), Indisponible (checkbox). Retirer le badge « Catalogue figé ».

#### 7. Web — Planning

- **`PlanningGrid.razor`** : afficher un badge « Indisponible » sur les studios marqués `Unavailable`.
- **`ReservationFormModal.razor`** : filtrer les studios indisponibles dans le dropdown de sélection.

#### 8. Tests

- Adapter tous les appels `Studio.Create()` existants (6+ fichiers) pour le nouveau paramètre.
- Ajouter tests : refus de réservation sur studio indisponible, commande `UpdateStudio`.

### Points d'attention

- Les réservations **existantes** sur un studio marqué indisponible restent visibles dans le planning (elles ne sont pas supprimées). Seule la **création** de nouvelles réservations est bloquée.
- Les KPIs d'occupation excluent les studios indisponibles pour refléter la réalité opérationnelle.
- Le `DisplayOrder` n'est pas modifiable via l'admin (ordre figé).

**Livrable** : Studios modifiables par l'admin, champ Indisponible verrouillant les réservations, indicateurs visuels dans le planning.

---

## Phase 14 : Rapport de réservations avec calcul de prix + export PDF

**Objectif** : Fournir aux administrateurs un rapport détaillé des réservations avec calcul de prix ligne par ligne, consultable pour un mois donné ou une année complète (y compris en cours). Export PDF du rapport.

### Analyse d'impact

| Couche | Fichiers impactés | Nature de l'impact |
|--------|-------------------|--------------------|
| **Domain** | `IReservationRepository.cs` | Ajout `GetByYearAsync(int year)` pour récupérer toutes les réservations d'une année |
| **Application** | Nouveau dossier `Queries/GetReservationReport/` | Query CQRS + Handler, nouveaux DTOs `ReservationReportDto`, `ReportLineDto`, `ReportSummaryDto` |
| **Application** | `DependencyInjection.cs` | Aucun changement (auto-registration des handlers) |
| **Infrastructure** | `ReservationRepository.cs` | Implémentation de `GetByYearAsync` |
| **Web** | `Admin.razor` | Nouvel onglet « Rapport » avec filtres (mois/année), tableau détaillé, bouton export PDF |
| **Web** | `EcbatanLocation.Web.csproj` | Ajout package QuestPDF Community |
| **Web** | Nouveau `Services/ReportPdfGenerator.cs` | Génération du document PDF via QuestPDF |
| **Tests** | Application.Tests | Tests du handler de rapport |

### Fonctionnalités du rapport

1. **Filtrage** :
   - Par **mois** (mois + année) : affiche les réservations qui chevauchent ce mois
   - Par **année complète** : affiche toutes les réservations de l'année, même si l'année n'est pas terminée (encours)
   - Choix via toggle « Mois » / « Année »

2. **Tableau détaillé** :
   - Studio, Locataire, Propriétaire, Dates, Nb jours, Lignes de personnes (type, adultes, enfants -3 ans), Tarif unitaire, Montant ligne, Montant total réservation, Statut
   - Tri par date de début

3. **Synthèse** :
   - Nombre total de réservations
   - Nombre total de nuitées
   - Montant total (toutes réservations)
   - Ventilation par statut (Demande / Acceptée / Confirmée)
   - Ventilation par propriétaire

4. **Export PDF** :
   - Bouton « Exporter PDF » génère et télécharge un document PDF formaté
   - En-tête : titre, période, date de génération
   - Corps : tableau des réservations avec détail prix
   - Pied : synthèse et totaux

### Tâches

#### 1. Domain — Repository

- **`IReservationRepository.cs`** — Ajouter : `Task<IReadOnlyList<Reservation>> GetByYearAsync(int year, CancellationToken ct = default)`.

#### 2. Infrastructure — Repository

- **`ReservationRepository.cs`** — Implémenter `GetByYearAsync` : filtre les réservations dont les dates chevauchent l'année (StartDate < 1er janvier N+1 ET EndDate > 1er janvier N).

#### 3. Application — DTOs

- **`ReportLineDto`** : `ReservationId`, `StudioName`, `OwnerName`, `TenantName`, `StartDate`, `EndDate`, `NumberOfDays`, `PersonLines` (type, adultes, enfants, tarif unitaire, montant ligne), `TotalAmount`, `Status`.
- **`ReportPersonLineDto`** : `ClientTypeLabel`, `AdultCount`, `ChildrenUnder3Count`, `RatePerDay`, `LineAmount`.
- **`ReportSummaryDto`** : `TotalReservations`, `TotalNights`, `TotalAmount`, `ByStatus` (dict statut→montant+count), `ByOwner` (dict owner→montant+count).
- **`ReservationReportDto`** : `Year`, `Month?`, `PeriodLabel`, `Lines`, `Summary`, `GeneratedAt`.

#### 4. Application — Query

- **`GetReservationReportQuery`** (`IRequireAdmin`) : `int Year`, `int? Month`.
- **`GetReservationReportQueryHandler`** :
  - Si `Month` est fourni → `GetByMonthAsync(year, month)`, sinon → `GetByYearAsync(year)`.
  - Charge la `PricingGrid` de l'année.
  - Pour chaque réservation : calcule le montant par ligne de personnes via `PricingGrid.CalculateAmount()`.
  - Charge studios et owners pour les noms.
  - Construit le `ReservationReportDto` avec lignes triées par date + synthèse.

#### 5. Web — Onglet Rapport

- **`Admin.razor`** — Ajouter un 5e onglet « Rapport » :
  - Toggle « Mois » / « Année » pour choisir la granularité.
  - Sélecteur mois + année (avec navigation ← →).
  - Bouton « Générer le rapport ».
  - Tableau des réservations avec détail prix.
  - Section synthèse (totaux, ventilation par statut et par propriétaire).
  - Bouton « Exporter PDF » (téléchargement via JS interop).

#### 6. Web — Génération PDF

- **Package** : ajouter `QuestPDF` au projet Web (licence Community, gratuit pour revenus < $1M).
- **`Services/ReportPdfGenerator.cs`** : service injectable qui prend un `ReservationReportDto` et retourne un `byte[]` PDF.
  - En-tête : logo/titre Ecbatan Location, période, date de génération.
  - Tableau : colonnes Studio, Locataire, Propriétaire, Dates, Jours, Détail personnes, Montant, Statut.
  - Synthèse : totaux, ventilations.
- **Endpoint** : route `/api/report/pdf?year=2026&month=7` protégée `[Authorize(Roles="Admin")]` retournant le fichier PDF.

#### 7. Tests

- **Application.Tests** : `GetReservationReportQueryHandler` — rapport mensuel, rapport annuel, calcul correct des montants, synthèse correcte, période sans réservations.

### Points d'attention

- **Réservations à cheval sur la période** : une réservation du 28 juin au 5 juillet apparaît dans les rapports de juin ET de juillet. Le montant affiché est le montant **total** de la réservation (pas un prorata).
- **Grille tarifaire manquante** : si aucune grille n'existe pour l'année, les montants sont affichés comme « N/A » et la synthèse les exclut.
- **QuestPDF Community** : licence gratuite pour usage non commercial ou revenus < $1M/an. Parfait pour ce projet.

**Livrable** : Onglet rapport dans l'admin avec tableau détaillé des réservations, calcul de prix, synthèse, et export PDF.

---

## Phase 17 : Capacité — Exclure les enfants de moins de 3 ans

**Objectif** : Les enfants de moins de 3 ans ne comptent plus dans la vérification de capacité d'un studio. Seuls les adultes sont comparés à la capacité maximale du logement.

### Règle métier modifiée

- **Avant** : `Adultes + Enfants -3 ans ≤ Capacité du studio`
- **Après** : `Adultes ≤ Capacité du studio`

Les enfants de moins de 3 ans restent enregistrés (pour la tarification et l'affichage) mais ne bloquent plus la réservation quand leur présence fait dépasser la capacité.

### Analyse d'impact

| Couche | Fichier | Nature de l'impact |
|--------|---------|-------------------|
| **Domain** | `Reservation.cs` | `ValidatePersonLines()` : remplacer `totalPersons` par `totalAdults` dans la comparaison à `studioCapacity` |
| **Tests** | `ReservationTests.cs` | 4 tests à adapter : `Create_CapacityExceeded_Throws`, `Create_ExactCapacity_Succeeds`, `Create_MultipleLines_CapacityExceeded_Throws`, `Update_CapacityExceeded_Throws` — les valeurs de test doivent provoquer le dépassement par les seuls adultes |
| **Doc** | `CLAUDE.md` | Mettre à jour la règle invariante de capacité |

### Tâches

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

### Ce qui ne change PAS

- `PersonLine.TotalPersons` (adultes + enfants) : conservé pour l'affichage et la tarification
- `Reservation.TotalPersonCount` : idem, compteur d'affichage
- Tarification : les enfants -3 ans restent facturés selon la grille tarifaire
- KPIs d'occupation : basés sur la capacité du studio, pas le nombre de personnes

**Livrable** : Validation de capacité basée uniquement sur les adultes, tests adaptés.

---

## Phase 18 : Durcissement authentification

**Objectif** : Corriger les faiblesses identifiées dans le système d'authentification — session persistante sans limite, absence de lockout sur échec de connexion, et absence de choix « Se souvenir de moi ».

### Problèmes identifiés

1. **Session sans fin** : le cookie expire après 30 jours avec sliding expiration (se réinitialise à chaque requête). Combiné à `isPersistent: true` codé en dur, l'utilisateur n'est **jamais** déconnecté tant qu'il utilise l'app régulièrement.
2. **Pas de lockout** : `lockoutOnFailure: false` dans le login — un attaquant peut tenter des mots de passe indéfiniment sans blocage du compte.
3. **Pas de checkbox « Se souvenir de moi »** : `isPersistent: true` est codé en dur — le cookie survit toujours à la fermeture du navigateur, même si l'utilisateur ne le souhaite pas.

### Tâches

#### 1. Réduire la durée du cookie et désactiver le sliding

- **`Program.cs`** — Modifier `ConfigureApplicationCookie` :
  - `ExpireTimeSpan` : `TimeSpan.FromDays(30)` → `TimeSpan.FromHours(2)` (2 heures)
  - `SlidingExpiration` : `true` → `false` (expiration absolue, pas de renouvellement automatique)
- **Effet** : reconnexion obligatoire toutes les 2 heures maximum, même en cas d'utilisation régulière.

#### 2. Activer le lockout sur échec de connexion

- **`DependencyInjection.cs`** (Infrastructure) — Ajouter la config lockout dans `AddIdentity` :
  - `options.Lockout.MaxFailedAccessAttempts = 5`
  - `options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15)`
  - `options.Lockout.AllowedForNewUsers = true`
- **`Login.razor`** — Passer `lockoutOnFailure: true` dans `PasswordSignInAsync`.
- **Effet** : après 5 tentatives échouées, le compte est verrouillé pendant 15 minutes.

#### 3. Ajouter la checkbox « Se souvenir de moi »

- **`Login.razor`** — Ajouter un champ `RememberMe` au `LoginModel` (booléen, défaut `false`).
- Ajouter une checkbox dans le formulaire.
- Passer `isPersistent: Input.RememberMe` au lieu de `isPersistent: true`.
- **Effet** : sans la case cochée, le cookie est un cookie de session (supprimé à la fermeture du navigateur). Avec la case, le cookie persiste 2 heures.

### Analyse d'impact

| Couche | Fichiers impactés | Nature de l'impact |
|--------|-------------------|--------------------|
| **Web** | `Program.cs` | Durée cookie réduite, sliding désactivé |
| **Web** | `Login.razor` | Checkbox « Se souvenir de moi », lockout activé |
| **Infrastructure** | `DependencyInjection.cs` | Config lockout Identity |

### Ce qui ne change PAS

- `IdentityRevalidatingAuthenticationStateProvider` : la revalidation du security stamp (30 min) reste en place.
- Logout (`/api/auth/logout`) : inchangé.
- Cookie sécurité (HttpOnly, Secure, SameSite=Strict) : inchangé.
- Mot de passe policy : inchangé.
- Headers de sécurité (CSP, HSTS, X-Frame-Options) : inchangés.

**Livrable** : Authentification durcie — cookie à durée fixe de 2 heures, lockout après 5 tentatives, choix « Se souvenir de moi ».

---

## Dépendances NuGet prévues

| Package | Projet | Usage |
|---------|--------|-------|
| FluentValidation | Application | Validation commands |
| FluentValidation.DependencyInjectionExtensions | Application | Auto-registration |
| Microsoft.Extensions.DependencyInjection.Abstractions | Application | DI (médiateur maison) |
| Microsoft.Extensions.Logging.Abstractions | Application | Logging (handlers d'events) |
| Microsoft.EntityFrameworkCore | Infrastructure | ORM |
| Microsoft.EntityFrameworkCore.Sqlite | Infrastructure | Provider SQLite |
| Npgsql.EntityFrameworkCore.PostgreSQL | Infrastructure | Provider PostgreSQL |
| Microsoft.EntityFrameworkCore.Tools | Infrastructure | Migrations |
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | Infrastructure | Identity |
| Microsoft.AspNetCore.Components.Authorization | Web | Auth Blazor |

> **CQRS** : assuré par un **médiateur maison** (`EcbatanLocation.Application/Messaging`), sans dépendance externe. MediatR a été retiré (passé sous licence commerciale en v13+) au profit d'une solution 100 % open source.

---

## Phase 15 : Migration déploiement Fly.io → Northflank

**Objectif** : Migrer l'hébergement de Fly.io (free tier supprimé en 2024, devenu payant) vers Northflank (free tier Developer Sandbox), tout en conservant le contrôle du déploiement via tags Git dans GitHub Actions.

### Contexte

Fly.io a supprimé son free tier permanent en 2024. Le plan actuel est pay-as-you-go avec un crédit d'essai de 5$ expirant en 30 jours. Northflank propose un free tier « Developer Sandbox » suffisant pour l'application :

| Ressource | Northflank Free Tier |
|-----------|---------------------|
| Services | 2 |
| CPU | 1 vCPU (partagé) |
| RAM | 1 GB |
| Volume persistant | 0.5 GB inclus |
| Build intégré | Oui (depuis GitHub) |
| Cold starts | Non |

Le volume de 0.5 GB est largement suffisant pour la base SQLite (quelques Mo).

### Stratégie de déploiement

**Choix retenu** : GitHub Actions avec API Northflank (et non le CI/CD intégré Northflank).

**Raison** : conserver le déclenchement par tag `v*` dans le workflow `release.yml`. Le CI/CD intégré Northflank se déclenche sur chaque push, sans contrôle fin sur le moment du déploiement. Avec GitHub Actions, on garde la maîtrise : seul un tag déclenche le pipeline tests → release → deploy.

### Tâches

#### 1. Configuration Northflank (manuelle, une seule fois)

- Créer un compte Northflank et un projet `ecbatan-location`.
- Connecter le dépôt GitHub au projet Northflank.
- Créer un service « Combined » (build + run) pointant vers le `Dockerfile`.
- Créer un volume persistant de 0.5 GB monté sur `/data`.
- Configurer les variables d'environnement : `ASPNETCORE_URLS=http://+:8080`, `ConnectionStrings__DefaultConnection=Data Source=/data/ecbatanlocation.db`.
- Récupérer les identifiants API : **API token**, **Project ID**, **Service ID**.

#### 2. Secrets GitHub

- Ajouter les secrets dans le dépôt GitHub (Settings > Secrets > Actions) :
  - `NORTHFLANK_API_TOKEN` : token API Northflank
  - `NORTHFLANK_PROJECT_ID` : ID du projet
  - `NORTHFLANK_SERVICE_ID` : ID du service

#### 3. Workflow GitHub Actions — déploiement conditionnel

- **Modifier `release.yml`** : rendre le job `deploy` conditionnel via une variable de repository ou un `workflow_dispatch` input.
- Ajouter un **input `target`** au workflow (`fly` ou `northflank`, défaut `northflank`).
- Le job `deploy` existant (Fly.io) est conservé dans un job `deploy-fly`, conditionné par `target == 'fly'`.
- Un nouveau job `deploy-northflank` est ajouté, conditionné par `target == 'northflank'`.
- Sur push de tag `v*`, le target par défaut (`northflank`) s'applique automatiquement.

```yaml
on:
  push:
    tags: ['v*']
  workflow_dispatch:
    inputs:
      target:
        description: 'Cible de déploiement'
        type: choice
        options: [northflank, fly]
        default: northflank

jobs:
  # ... setup, test, publish, release inchangés ...

  deploy-northflank:
    if: inputs.target != 'fly'
    # Appel API Northflank pour déclencher un build+deploy du commit tagué

  deploy-fly:
    if: inputs.target == 'fly'
    # Job Fly.io existant (conservé pour rollback ou usage ponctuel)
```

#### 4. Job `deploy-northflank` — détail

- Utilise l'API REST Northflank pour déclencher un pipeline de déploiement :
  1. Créer un build via `POST /v1/projects/{projectId}/services/{serviceId}/builds` avec la référence Git (tag).
  2. Attendre la fin du build (polling status ou webhook).
- Alternative : utiliser la CLI Northflank (`npx @northflank/cli`) si disponible.

#### 5. Documentation

- Créer `docs/deploiement-northflank.md` sur le modèle de `docs/deploiement-flyio.md` :
  - Prérequis (compte Northflank, CLI optionnel)
  - Installation initiale (création projet, service, volume)
  - Configuration GitHub Actions (secrets)
  - Commandes utiles (logs, backup SQLite)
  - Limites du free tier

#### 6. Nettoyage (après validation)

- Retirer `fly.toml` de la branche principale (conserver dans l'historique Git).
- Mettre à jour le `README.md` si nécessaire.
- Supprimer le secret `FLY_API_TOKEN` du dépôt GitHub (après confirmation que Northflank fonctionne).

### Dockerfile

Le `Dockerfile` existant est **compatible tel quel** avec Northflank. Aucune modification nécessaire :
- Port 8080 exposé ✅
- Variable `ConnectionStrings__DefaultConnection` pointant vers `/data/` ✅
- Image multi-stage (SDK build + runtime) ✅

**Livrable** : Déploiement automatique sur Northflank via tag Git, job Fly.io conservé en fallback, documentation de déploiement.

---

## Phase 16 : Support double base de données SQLite / PostgreSQL

**Objectif** : Permettre de basculer entre SQLite (développement local, tests) et PostgreSQL (production Northflank) via un simple paramètre de configuration, sans impacter le code applicatif.

### Contexte

Le déploiement sur Northflank (Phase 15) utilise une base de données PostgreSQL managée au lieu d'un fichier SQLite sur volume persistant. Le code applicatif (LINQ pur, aucun SQL brut) est déjà provider-agnostic grâce à EF Core. Seule la couche de configuration DI et les fichiers de settings nécessitent des modifications.

### Modifications réalisées

#### 1. Package NuGet

- **`EcbatanLocation.Infrastructure.csproj`** — Ajout de `Npgsql.EntityFrameworkCore.PostgreSQL` v10.0.2 en plus de `Microsoft.EntityFrameworkCore.Sqlite` (les deux providers coexistent).

#### 2. DI — Switch de provider

- **`DependencyInjection.cs`** — Le paramètre `DatabaseProvider` (lu depuis `IConfiguration`) détermine le provider EF Core :
  - `"Sqlite"` (défaut) → `.UseSqlite(connectionString)`
  - `"PostgreSQL"` → `.UseNpgsql(connectionString)`

#### 3. Configuration

- **`appsettings.json`** (développement) — Ajout de `"DatabaseProvider": "Sqlite"`. Connection string SQLite inchangée.
- **`appsettings.Production.json`** — `"DatabaseProvider": "PostgreSQL"` avec connection string PostgreSQL template (`Host=...;Port=5432;Database=...;Username=...;Password=...`).
- **`Dockerfile`** — Remplacement de la connection string SQLite en dur par `ENV DatabaseProvider="PostgreSQL"`. La connection string est fournie par variable d'environnement Northflank (`ConnectionStrings__DefaultConnection`).

#### 4. Migrations

Les migrations existantes (SQLite) restent en place pour le développement local. Pour PostgreSQL en production, `EnsureCreated()` / `Database.Migrate()` via le `DbInitializer` gère la création du schéma. Les annotations `Sqlite:Autoincrement` sont ignorées par Npgsql (EF Core utilise automatiquement `SERIAL`/`BIGSERIAL`).

#### 5. Tests

Les tests unitaires et d'intégration continuent d'utiliser SQLite in-memory (`TestDbContextFactory`) et SQLite fichier (`IntegrationTestFixture`). Aucune modification nécessaire — les tests vérifient la logique métier, pas le provider de base.

### Ce qui n'a PAS changé (et pourquoi)

| Élément | Raison |
|---------|--------|
| DbContext (`EcbatanLocationDbContext`) | `ApplyConfigurationsFromAssembly` est provider-agnostic |
| Entity Configurations (Fluent API) | Portables entre SQLite et PostgreSQL |
| Repositories (100% LINQ) | Aucun SQL brut, aucune fonction spécifique à un provider |
| DbInitializer (seed) | `AddAsync` / `SaveChangesAsync` fonctionnent identiquement |
| Domain | Aucune dépendance infrastructure |
| Enum → string conversion | Supporté nativement par PostgreSQL |
| Decimal precision `(10, 2)` | Supporté nativement par PostgreSQL (`NUMERIC`) |
| Transactions explicites | `BeginTransactionAsync()` fonctionne sur les deux providers |

### Configuration Northflank (variables d'environnement)

```
DatabaseProvider=PostgreSQL
ConnectionStrings__DefaultConnection=Host=<addon-host>;Port=5432;Database=<addon-db>;Username=<addon-user>;Password=<addon-password>
```

Ces valeurs sont fournies automatiquement par l'addon PostgreSQL de Northflank et configurées dans les variables d'environnement du service.

**Livrable** : Application compatible SQLite et PostgreSQL, switch configurable via `DatabaseProvider` dans les settings.
