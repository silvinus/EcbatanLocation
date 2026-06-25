---
layout: default
title: Documentation technique
nav_order: 3
---

# Documentation technique
{: .fs-8 }

Architecture, conventions et principes du projet.
{: .fs-5 .fw-300 }

---

## Architecture de la solution

```
EcbatanLocation.sln
├── src/
│   ├── EcbatanLocation.Domain/           # Entites, Value Objects, Regles metier
│   ├── EcbatanLocation.Application/      # Commands, Queries, Handlers, DTOs
│   ├── EcbatanLocation.Infrastructure/   # EF Core, Repositories, Identity, Migrations
│   └── EcbatanLocation.Web/              # Blazor Server, Pages, Composants
└── tests/
    ├── EcbatanLocation.Domain.Tests/
    ├── EcbatanLocation.Application.Tests/
    └── EcbatanLocation.Infrastructure.Tests/
```

## Stack technique

| Couche | Technologie |
|--------|------------|
| Frontend | Blazor Server (.NET 10) |
| Backend | ASP.NET Core 10 |
| Authentification | ASP.NET Identity |
| Base de donnees | SQLite (dev) / PostgreSQL (prod) via EF Core, switch via `DatabaseProvider` |
| Architecture | DDD + CQRS (mediateur interne) |
| Tests | xUnit + FluentAssertions |
| CI/CD | GitHub Actions |
| Deploiement | Docker / GHCR → Northflank (ou VPS Linux) |

---

## Domain-Driven Design (DDD)

Le projet suit les principes DDD avec une separation stricte des couches :

### Couche Domain

Aucune dependance externe. Contient :

- **Entites riches** : logique metier directement dans les entites (constructeurs prives + methodes de fabrique)
- **Value Objects** : records C# immutables (ex: `DateRange`, `PersonCount`)
- **Interfaces de repository** : contrats definis dans le domaine, implementes dans l'infrastructure
- **Services de domaine** : logique metier impliquant plusieurs aggregats (ex: `ReservationDomainService`)

### Couche Application

Depend uniquement de Domain. Contient :

- **Commands** : operations qui modifient l'etat (creer/modifier reservation, changer statut)
- **Queries** : operations de lecture (planning mensuel, detail reservation, KPIs)
- **Handlers** : executent les commands/queries via le mediateur interne
- **DTOs** : objets de transfert pour les vues
- **Validators** : validation des entrees avec FluentValidation

### Couche Infrastructure

Implemente les interfaces du Domain. Contient :

- **DbContext** : configuration EF Core, fournisseur SQLite ou PostgreSQL selon `DatabaseProvider`
- **Repositories concrets** : implementations des interfaces de repository
- **Identity** : configuration ASP.NET Identity (authentification, roles)
- **Migrations** : historique des migrations de schema, separe par fournisseur (assemblies `EcbatanLocation.Infrastructure.Migrations.Sqlite` et `.PostgreSQL`)
- **Seeding** : donnees initiales (studios, proprietaires, tarifs)

### Couche Web

Point d'entree de l'application. Contient :

- **Pages Blazor** : composants de page (Planning, Login, Admin)
- **Composants** : composants reutilisables (calendrier, formulaires, modales)
- **Program.cs** : configuration de l'injection de dependances
- **wwwroot** : assets statiques (CSS, JS, images)

---

## CQRS via mediateur interne

Le projet utilise un **mediateur interne** (`IMediator` / `Mediator` dans `Application/Messaging`), pas la librairie MediatR. Toute interaction metier passe par une Command ou une Query :

```csharp
// Command (modification)
await _mediator.Send(new CreateReservationCommand { ... });

// Query (lecture)
var planning = await _mediator.Send(new GetMonthlyPlanningQuery { ... });
```

Les handlers ne sont jamais appeles directement depuis les composants Blazor. Cela garantit :

- La separation des responsabilites
- La testabilite (chaque handler est testable independamment)
- La tracabilite des operations

### Isolation du DbContext (Blazor Server)

Le circuit Blazor Server est long-lived et peut rendre plusieurs composants en parallele. Un `DbContext` scoped partage provoquerait l'erreur *« A second operation was started on this context instance »*. Le mediateur execute donc **chaque handler dans un scope DI enfant frais** (`IServiceScopeFactory.CreateScope()`) : chaque Command/Query obtient son propre `DbContext`, isole des operations concurrentes.

**Invariant** : les repositories (et `DbContext`) ne sont injectes que dans des handlers, jamais directement dans un composant Blazor.

### Domain events — dispatch post-commit

Les domain events sont collectes pendant `SaveChanges`, puis dispatches par le mediateur **apres le commit** de la transaction. Les handlers de domain events sont des reactions post-commit, best-effort (notifier, logger, auditer) : ils se declenchent uniquement si l'operation a reussi, et une exception dans un handler est loggee mais ne fait pas echouer l'operation deja committee. Les invariants metier (chevauchement, capacite, dependance studio) sont enforces synchroniquement dans l'agregat, le repository et les contraintes BDD.

---

## Conventions de code

| Regle | Detail |
|-------|--------|
| Langue du code | Anglais (classes, methodes, variables, commentaires) |
| Nommage | PascalCase (classes, methodes), camelCase (variables locales) |
| Entites | Constructeurs prives + methodes de fabrique |
| Value Objects | Records C# immutables |
| Logique metier | Jamais dans les pages Blazor ou controllers |
| Organisation | Un fichier par classe/record/enum |
| Tests | Unitaires pour les regles metier du domaine |

---

## Tests

Le projet comporte 3 projets de tests :

| Projet | Scope |
|--------|-------|
| `Domain.Tests` | Regles metier, entites, value objects |
| `Application.Tests` | Handlers, integration avec la vraie stack serveur |
| `Infrastructure.Tests` | Repositories, DbContext, persistance |

Les tests d'application sont des **tests d'integration** qui utilisent une vraie instance du serveur via `WebApplicationFactory`, avec une base SQLite in-memory.

### Execution

```bash
dotnet test
```

### CI

Les tests sont executes automatiquement par GitHub Actions :
- Sur chaque push sur `main`
- Sur chaque pull request vers `main`
- Avant la publication d'une release

---

## CI/CD

### Workflow CI (`ci.yml`)

Declanche sur push et PR vers `main`. Deux jobs paralleles :

1. **Build** : restore, format check, build, tests
2. **Security** : audit des packages vulnerables

### Workflow Release (`release.yml`)

Declanche sur les tags `v*` (ou via `workflow_dispatch`). Jobs sequentiels :

1. **Setup** : extraction de la version depuis le tag
2. **Test** : execution de tous les tests
3. **Publish** : build self-contained linux-x64
4. **Release** : creation de la GitHub Release avec l'archive
5. **Docker** : build de l'image et push sur GitHub Container Registry (GHCR)
6. **Deploy** : redeploiement sur Northflank (cible par defaut) ou Fly.io (fallback)

### Dependabot

Configuration automatique pour :
- **NuGet** : mise a jour hebdomadaire (lundi), groupement minor+patch
- **GitHub Actions** : mise a jour des actions, groupees ensemble

---

## Hypotheses de travail

Ces points sont isoles dans le code (methode dediee, point unique de modification) pour pouvoir etre changes facilement :

### H1 — Studio non louable seul

Un studio `RentableAlone = false` est relie **explicitement** a une reservation parent sur un studio `RentableAlone = true` du meme proprietaire. Les dates du parent doivent **englober entierement** celles de l'enfant (inclusion stricte, pas simple chevauchement) et le statut du parent est propage vers ses enfants.

**Point de modification** : `ReservationDomainService.ValidateParentLink()` + `DateRange.Contains()`

### H2 — Places occupees

Places occupees = capacite max des studios ayant au moins une reservation Acceptee ou Confirmee ce jour-la (pas les Demandes). Un studio est compte en entier (libre ou occupe).

**Point de modification** : `GetDailyOccupationQueryHandler`

### H3 — Inclusivite des dates

Jour d'arrivee inclus, jour de depart exclu (logique nuitee). Du 3 au 10 = 7 nuits facturees. Le studio est libre le jour du depart.

**Point de modification** : `DateRange` (value object)

### H4 — Infos visibles en mode public

Le public voit toutes les infos de la reservation (nom locataire, proprietaire, nombre de personnes, statut).

**Point de modification** : `GetMonthlyPlanningQueryHandler` (mapping du DTO de planning)
