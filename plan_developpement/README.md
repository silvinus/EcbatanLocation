# Plan de développement - Ecbatan Location

## Vue d'ensemble

Le développement est découpé en **21 phases** progressives, chaque phase livrant un incrément fonctionnel testable.

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
| **19** | **Lien explicite parent-enfant entre réservations** | **✅ Livrée** |
| **20** | **Unit of Work transactionnel & dispatch critique des domain events** | **À faire** |
| **21** | **Location au lit (per-bed) pour certains logements** | **✅ Livrée** |

## Fichiers

- [Phase 1 — Scaffolding & Infrastructure de base](phase-01.md)
- [Phase 2 — Couche Domain - Modèle métier](phase-02.md)
- [Phase 3 — Couche Application - Commands & Queries](phase-03.md)
- [Phase 4 — Couche Infrastructure - Persistence](phase-04.md)
- [Phase 5 — UI Blazor - Planning & Lecture publique](phase-05.md)
- [Phase 6 — UI Blazor - Authentification & Édition propriétaire](phase-06.md)
- [Phase 7 — Finalisation & Déploiement](phase-07.md)
- [Phase 8 — Multi-typologies de personnes par réservation](phase-08.md)
- [Phase 9 — Durcissement (sécurité, intégrité, architecture, UI)](phase-09.md)
- [Phase 10 — Bugfix — Règle « studio non louable seul » (H1)](phase-10.md)
- [Phase 11 — Gestion des utilisateurs (Admin)](phase-11.md)
- [Phase 12 — Types client flexibles Mobil-home / Tente](phase-12.md)
- [Phase 13 — Gestion admin des studios + champ Indisponible](phase-13.md)
- [Phase 14 — Rapport de réservations avec calcul de prix + export PDF](phase-14.md)
- [Phase 15 — Migration déploiement Fly.io → Northflank](phase-15.md)
- [Phase 16 — Support double base de données SQLite / PostgreSQL](phase-16.md)
- [Phase 17 — Capacité — Exclure les enfants de moins de 3 ans](phase-17.md)
- [Phase 18 — Durcissement authentification](phase-18.md)
- [Phase 19 — Lien explicite parent-enfant entre réservations](phase-19.md)
- [Phase 20 — Unit of Work transactionnel & dispatch critique des domain events](phase-20.md)
- [Phase 21 — Location au lit (per-bed) pour certains logements](phase-21.md)

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
