# Phase 16 : Support double base de données SQLite / PostgreSQL

**Objectif** : Permettre de basculer entre SQLite (développement local, tests) et PostgreSQL (production Northflank) via un simple paramètre de configuration, sans impacter le code applicatif.

## Contexte

Le déploiement sur Northflank (Phase 15) utilise une base de données PostgreSQL managée au lieu d'un fichier SQLite sur volume persistant. Le code applicatif (LINQ pur, aucun SQL brut) est déjà provider-agnostic grâce à EF Core. Seule la couche de configuration DI et les fichiers de settings nécessitent des modifications.

## Modifications réalisées

### 1. Package NuGet

- **`EcbatanLocation.Infrastructure.csproj`** — Ajout de `Npgsql.EntityFrameworkCore.PostgreSQL` v10.0.2 en plus de `Microsoft.EntityFrameworkCore.Sqlite` (les deux providers coexistent).

### 2. DI — Switch de provider

- **`DependencyInjection.cs`** — Le paramètre `DatabaseProvider` (lu depuis `IConfiguration`) détermine le provider EF Core :
  - `"Sqlite"` (défaut) → `.UseSqlite(connectionString)`
  - `"PostgreSQL"` → `.UseNpgsql(connectionString)`

### 3. Configuration

- **`appsettings.json`** (développement) — Ajout de `"DatabaseProvider": "Sqlite"`. Connection string SQLite inchangée.
- **`appsettings.Production.json`** — `"DatabaseProvider": "PostgreSQL"` avec connection string PostgreSQL template (`Host=...;Port=5432;Database=...;Username=...;Password=...`).
- **`Dockerfile`** — Remplacement de la connection string SQLite en dur par `ENV DatabaseProvider="PostgreSQL"`. La connection string est fournie par variable d'environnement Northflank (`ConnectionStrings__DefaultConnection`).

### 4. Migrations

Les migrations existantes (SQLite) restent en place pour le développement local. Pour PostgreSQL en production, `EnsureCreated()` / `Database.Migrate()` via le `DbInitializer` gère la création du schéma. Les annotations `Sqlite:Autoincrement` sont ignorées par Npgsql (EF Core utilise automatiquement `SERIAL`/`BIGSERIAL`).

### 5. Tests

Les tests unitaires et d'intégration continuent d'utiliser SQLite in-memory (`TestDbContextFactory`) et SQLite fichier (`IntegrationTestFixture`). Aucune modification nécessaire — les tests vérifient la logique métier, pas le provider de base.

## Ce qui n'a PAS changé (et pourquoi)

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

## Configuration Northflank (variables d'environnement)

```
DatabaseProvider=PostgreSQL
ConnectionStrings__DefaultConnection=Host=<addon-host>;Port=5432;Database=<addon-db>;Username=<addon-user>;Password=<addon-password>
```

Ces valeurs sont fournies automatiquement par l'addon PostgreSQL de Northflank et configurées dans les variables d'environnement du service.

**Livrable** : Application compatible SQLite et PostgreSQL, switch configurable via `DatabaseProvider` dans les settings.
