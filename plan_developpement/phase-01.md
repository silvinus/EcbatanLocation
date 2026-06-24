# Phase 1 : Scaffolding & Infrastructure de base

**Objectif** : Solution compilable avec l'architecture DDD en place, base SQLite fonctionnelle, Identity configuré.

## Tâches

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
