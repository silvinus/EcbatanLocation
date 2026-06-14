# Ecbatan Location - Application de gestion de location saisonnière

## Contexte projet

Application web de gestion d'un planning de location pour une maison de vacances partagée entre 4 copropriétaires.
Le planning est consultable publiquement (lecture seule) et éditable par les propriétaires authentifiés.

## Stack technique

| Couche | Technologie |
|--------|------------|
| Frontend | Blazor Server (.NET 10) |
| Backend | ASP.NET Core 10 |
| Authentification | ASP.NET Identity |
| Base de données | SQLite (dev) / PostgreSQL (prod), switch via `DatabaseProvider` dans settings |
| Architecture | DDD + CQRS (MediatR) |
| Déploiement | Standalone sur VPS Linux (pas de microservices) |

## Architecture de la solution

```
EcbatanLocation.sln
├── src/
│   ├── EcbatanLocation.Domain/           # Entités, Value Objects, Règles métier, Interfaces repos
│   ├── EcbatanLocation.Application/      # Commands, Queries, Handlers (MediatR), DTOs, Validators
│   ├── EcbatanLocation.Infrastructure/   # EF Core DbContext, Repositories, Identity config, Migrations
│   └── EcbatanLocation.Web/              # Blazor Server, Pages, Components, Program.cs
└── tests/
    ├── EcbatanLocation.Domain.Tests/
    ├── EcbatanLocation.Application.Tests/
    └── EcbatanLocation.Infrastructure.Tests/
```

### Principes DDD

- **Domain** : aucune dépendance externe. Contient les entités riches (avec logique métier), les value objects, les interfaces de repository, les domain events.
- **Application** : dépend uniquement de Domain. Contient les use cases (Commands/Queries via MediatR), les DTOs, les validators (FluentValidation).
- **Infrastructure** : implémente les interfaces de Domain. Contient EF Core, les repositories concrets, la config Identity, le seeding des données.
- **Web** : point d'entrée. Blazor Server, injection de dépendances, pages et composants UI.

### CQRS via MediatR

- Toute interaction métier passe par une Command ou une Query envoyée via `IMediator`.
- Les Commands modifient l'état (créer/modifier réservation, changer statut).
- Les Queries lisent l'état (planning mensuel, détail réservation, KPIs).
- Les handlers ne sont jamais appelés directement depuis les composants Blazor.

## Modèle métier

### Hébergements (Studios) - Catalogue figé

| Nom | Capacité | Cuisine | Louable seul |
|-----|----------|---------|-------------|
| Villa | 6 | Oui | Oui |
| Studio Est | 2 | Oui | Oui |
| Studio Ouest | 2 | Oui | Oui |
| Studio Centre | 2 | Non | Non |
| Mobil-home | 6 | Non | Non |
| Emplacement tente 1 | 4 | Non | Oui |
| Emplacement tente 2 | 4 | Non | Oui |

### Propriétaires (4 fixes)

Léa, Sarah, Jean, Christophe. Chacun a un compte Identity.

### Réservation (Aggregate Root)

Champs :
- Studio (référence)
- DateDebut / DateFin
- Propriétaire (qui a créé)
- Locataire (nom/prénom)
- NbAdultes
- NbEnfantsMoins3Ans
- TypeClient (enum : Proprietaire, InviteAvecPresence, Connaissance, MobilHome, Tente)
- Statut (enum : Demande, Acceptee, Confirmee)
- AcceptéePar / AcceptéeLe (traçabilité)
- CreéeLe / ModifiéeLe

Règles invariantes :
- Un studio est **libre ou occupé** (pas de location partielle)
- **Aucun chevauchement** de réservations sur un même studio pour des dates qui se croisent
- Un studio **non louable seul** ne peut être réservé que conjointement avec un studio indépendant sur les mêmes dates par le même propriétaire
- Capacité : Adultes + Enfants ≤ Capacité du studio

### Tarification (versionnée par année)

Grille 2026 (prix/jour/personne) :

| Type client | Tarif |
|------------|-------|
| Propriétaire (et familles) | 3.50 € |
| Invités avec présence | 7.00 € |
| Connaissances | 15.00 € |
| Connaissances -3 ans (50%) | 7.50 € |
| Mobil-home | 12.00 € |
| Tente | 7.00 € |

La grille est modifiable par un admin et versionnée chaque année.

### Statuts de réservation

| Statut | Couleur | Description |
|--------|---------|-------------|
| Demande | Orange (#ffb020) | Réservation en attente de validation |
| Acceptée | Bleu (#6ea8ff) | Validée par un propriétaire |
| Confirmée | Vert (#27c48b) | Confirmation finale |

Transition : Demande → Acceptée → Confirmée. Chaque transition enregistre qui + quand.

## Rôles et droits

| Rôle | Droits |
|------|--------|
| Public (anonyme) | Lecture planning, voir réservations (infos limitées) |
| Propriétaire | Créer, modifier, changer statut des réservations |
| Admin (optionnel) | Gestion studios, tarifs, comptes propriétaires |

## Écrans

1. **Planning public** : vue mensuelle (studios en lignes, jours en colonnes), couleurs par statut, KPIs occupation
2. **Connexion** : formulaire email/mot de passe (Identity)
3. **Planning propriétaire** : même vue + boutons d'action (nouvelle réservation, édition, changement statut)
4. **Formulaire réservation** : modal avec tous les champs, contrôles de validation
5. **Gestion tarifs** (admin) : édition grille tarifaire annuelle

## UI / Design

- Thème sombre (voir maquettes HTML dans le dossier)
- Responsive (PC / tablette)
- Couleurs de statut figées (orange/bleu/vert)
- Filtres : mois, studio, statut, propriétaire
- Vues : Mois (prioritaire), Semaine, Liste

## Conventions de code

- **Tout le code est en anglais** : noms de classes, propriétés, méthodes, variables, commentaires, messages d'erreur. Seul le CLAUDE.md et la documentation restent en français.
- Nommage C# standard (PascalCase classes/méthodes, camelCase variables locales)
- Entités : constructeurs privés + méthodes de fabrique / méthodes métier
- Value Objects : records C# immutables
- Pas de logique métier dans les controllers/pages Blazor
- Un fichier par classe/record/enum
- Tests unitaires pour les règles métier du domaine

## Workflow Git

La branche `main` est protégée. Toute modification doit passer par une branche + Pull Request :

1. Créer une branche depuis `main` : `git checkout -b <type>/<description-courte>`
2. Commiter les changements
3. Pousser la branche : `git push -u origin <branche>`
4. Ouvrir une PR via `gh pr create`
5. Le propriétaire merge la PR depuis GitHub

Convention de nommage des branches : `fix/`, `feat/`, `refactor/`, `docs/`, `chore/`.

## Commandes utiles

```bash
# Créer la solution
dotnet new sln -n EcbatanLocation

# Restaurer les dépendances
dotnet restore

# Lancer l'application
dotnet run --project src/EcbatanLocation.Web

# Migrations EF Core
dotnet ef migrations add <NomMigration> --project src/EcbatanLocation.Infrastructure --startup-project src/EcbatanLocation.Web
dotnet ef database update --project src/EcbatanLocation.Infrastructure --startup-project src/EcbatanLocation.Web

# Tests
dotnet test
```

## Hypothèses de travail (à confirmer avec le client)

Ces points sont volontairement isolés dans le code (méthode dédiée, point unique de modification) pour pouvoir être changés sans refactoring.

### H1 — Studio non louable seul
Un studio `LouableSeul = false` ne peut être réservé que si le même propriétaire possède déjà une réservation sur un studio `LouableSeul = true` dont les dates **englobent entièrement** la période demandée (inclusion stricte, pas simple chevauchement).
- **Point de modification** : `ReservationDomainService.ValidateStudioDependency()` + `DateRange.Contains()`

### H2 — Places occupées
Places occupées = capacité max des studios ayant au moins une réservation Acceptée ou Confirmée ce jour-là (pas les Demandes). Un studio est compté en entier (libre ou occupé).
- **Point de modification** : `GetOccupationJourQueryHandler`

### H3 — Inclusivité des dates
Jour d'arrivée inclus, jour de départ exclu (logique nuitée). Du 3 au 10 = 7 nuits facturées. Le studio est libre le jour du départ.
- **Point de modification** : `DateRange` (value object unique pour toute la logique de dates)

### H4 — Infos visibles en mode public
Le public voit toutes les infos de la réservation (nom locataire, propriétaire, nombre de personnes, statut), comme sur la maquette.
- **Point de modification** : `PlanningMensuelDtoMapper`
