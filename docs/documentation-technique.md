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
| Base de donnees | SQLite (via EF Core) |
| Architecture | DDD + CQRS (MediatR) |
| Tests | xUnit + FluentAssertions |
| CI/CD | GitHub Actions |
| Deploiement | Standalone sur VPS Linux |

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
- **Handlers** : executent les commands/queries via MediatR
- **DTOs** : objets de transfert pour les vues
- **Validators** : validation des entrees avec FluentValidation

### Couche Infrastructure

Implemente les interfaces du Domain. Contient :

- **DbContext** : configuration EF Core avec SQLite
- **Repositories concrets** : implementations des interfaces de repository
- **Identity** : configuration ASP.NET Identity (authentification, roles)
- **Migrations** : historique des migrations de schema
- **Seeding** : donnees initiales (studios, proprietaires, tarifs)

### Couche Web

Point d'entree de l'application. Contient :

- **Pages Blazor** : composants de page (Planning, Login, Admin)
- **Composants** : composants reutilisables (calendrier, formulaires, modales)
- **Program.cs** : configuration de l'injection de dependances
- **wwwroot** : assets statiques (CSS, JS, images)

---

## CQRS via MediatR

Toute interaction metier passe par une Command ou une Query envoyee via `IMediator` :

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

Declanche sur les tags `v*`. Quatre jobs sequentiels :

1. **Setup** : extraction de la version depuis le tag
2. **Test** : execution de tous les tests
3. **Publish** : build self-contained linux-x64
4. **Release** : creation de la GitHub Release avec l'archive

### Dependabot

Configuration automatique pour :
- **NuGet** : mise a jour hebdomadaire (lundi), groupement minor+patch
- **GitHub Actions** : mise a jour des actions, groupees ensemble

---

## Hypotheses de travail

Ces points sont isoles dans le code (methode dediee, point unique de modification) pour pouvoir etre changes facilement :

### H1 — Studio non louable seul

Un studio `LouableSeul = false` ne peut etre reserve que si le meme proprietaire possede deja une reservation sur un studio `LouableSeul = true` dont les dates chevauchent (meme partiellement).

**Point de modification** : `ReservationDomainService.ValidateStudioDependency()`

### H2 — Places occupees

Places occupees = capacite max des studios ayant au moins une reservation Acceptee ou Confirmee ce jour-la (pas les Demandes). Un studio est compte en entier (libre ou occupe).

**Point de modification** : `GetOccupationJourQueryHandler`

### H3 — Inclusivite des dates

Jour d'arrivee inclus, jour de depart exclu (logique nuitee). Du 3 au 10 = 7 nuits facturees. Le studio est libre le jour du depart.

**Point de modification** : `DateRange` (value object)

### H4 — Infos visibles en mode public

Le public voit toutes les infos de la reservation (nom locataire, proprietaire, nombre de personnes, statut).

**Point de modification** : `PlanningMensuelDtoMapper`
