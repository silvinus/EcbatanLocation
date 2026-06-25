# Ecbatan Location

[![CI](https://github.com/silvinus/EcbatanLocation/actions/workflows/ci.yml/badge.svg)](https://github.com/silvinus/EcbatanLocation/actions/workflows/ci.yml)
[![Release](https://github.com/silvinus/EcbatanLocation/actions/workflows/release.yml/badge.svg)](https://github.com/silvinus/EcbatanLocation/actions/workflows/release.yml)
![.NET 10](https://img.shields.io/badge/.NET-10.0-512bd4)
![License](https://img.shields.io/badge/license-private-gray)

Application web de gestion d'un planning de location saisonnière pour une maison de vacances partagée entre 4 copropriétaires (Léa, Sarah, Jean, Christophe).

Le planning est consultable publiquement en lecture seule et éditable par les propriétaires authentifiés.

## Stack technique

| Couche | Technologie |
|--------|------------|
| Frontend | Blazor Server (.NET 10) |
| Backend | ASP.NET Core 10 |
| Authentification | ASP.NET Identity (lockout, cookie 2h, remember me) |
| Base de données | SQLite (dev) / PostgreSQL (prod) — switch via `DatabaseProvider` |
| Architecture | DDD + CQRS (médiateur interne, scope DI par handler) |
| Tests | xUnit + FluentAssertions |
| Déploiement | Docker / GitHub Container Registry → Northflank (ou VPS Linux) |

## Fonctionnalités

### Planning de location

- **Vue mensuelle** : studios en lignes, jours en colonnes, code couleur par statut
- **Vues alternatives** : semaine et liste
- **Vue agenda mobile** : affichage adapté détecté automatiquement selon la taille d'écran
- **Filtres** : par mois, studio, statut, propriétaire (panneau en overlay)
- **KPIs d'occupation** : taux d'occupation, places occupées par jour, code couleur de disponibilité sur les jours

### Gestion des réservations

- Création, modification et suivi des réservations par les propriétaires
- Workflow de validation : Demande (orange) → Acceptée (bleu) → Confirmée (vert)
- Contrôle des chevauchements (un studio est libre ou occupé, pas de location partielle)
- Vérification de capacité (adultes ≤ capacité du studio ; les enfants de moins de 3 ans ne comptent pas)
- Studios dépendants reliés explicitement à une réservation parent (les dates du parent doivent englober celles de l'enfant ; le statut est propagé du parent vers ses enfants)

### Rapport de réservations

- Synthèse des réservations avec calcul de prix par propriétaire et par statut
- Export PDF du rapport

### Catalogue d'hébergements

7 hébergements avec capacités et caractéristiques variées : Villa, Studios (Est, Ouest, Centre), Mobil-home et Emplacements tente.

### Tarification

- Grille tarifaire versionnée par année (prix/jour/personne)
- Tarifs différenciés par type de client : propriétaire, invité, connaissance, mobil-home, tente
- Tarif réduit pour les enfants de moins de 3 ans

### Accès et rôles

| Rôle | Droits |
|------|--------|
| Public (anonyme) | Consultation du planning en lecture seule |
| Propriétaire | Création, modification, changement de statut des réservations, rapport |
| Admin | Gestion des studios (CRUD), grille tarifaire, comptes propriétaires |

### Interface

- Thème sombre, responsive (PC / tablette)

## Architecture

```
EcbatanLocation.sln
├── src/
│   ├── EcbatanLocation.Domain/           # Entités, Value Objects, règles métier
│   ├── EcbatanLocation.Application/      # Commands, Queries, Handlers, DTOs
│   ├── EcbatanLocation.Infrastructure/   # EF Core, Repositories, Identity, Migrations
│   └── EcbatanLocation.Web/              # Blazor Server, Pages, Composants
└── tests/
    ├── EcbatanLocation.Domain.Tests/
    ├── EcbatanLocation.Application.Tests/
    └── EcbatanLocation.Infrastructure.Tests/
```

L'application suit les principes DDD (Domain-Driven Design) avec une séparation CQRS :
- Les **Commands** modifient l'état (créer/modifier une réservation, changer un statut)
- Les **Queries** lisent l'état (planning mensuel, détail d'une réservation, KPIs)

## Prérequis

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Compilation et lancement

```bash
# Restaurer les dépendances
dotnet restore

# Lancer l'application en développement
dotnet run --project src/EcbatanLocation.Web
```

L'application est accessible sur `https://localhost:5001` (ou le port configuré).

### Migrations EF Core

```bash
# Ajouter une migration
dotnet ef migrations add <NomMigration> \
  --project src/EcbatanLocation.Infrastructure \
  --startup-project src/EcbatanLocation.Web

# Appliquer les migrations
dotnet ef database update \
  --project src/EcbatanLocation.Infrastructure \
  --startup-project src/EcbatanLocation.Web
```

### Tests

```bash
dotnet test
```

## Déploiement

Deux cibles de déploiement sont supportées :

- **Northflank (cible par défaut)** : image Docker poussée sur GitHub Container Registry (GHCR), tirée et redéployée par Northflank. Voir [docs/deploiement-northflank.md](docs/deploiement-northflank.md).
- **VPS Linux (alternative)** : exécutable self-contained linux-x64 derrière Nginx (reverse proxy) avec SSL Let's Encrypt. Voir [docs/GUIDE_DEPLOIEMENT.md](docs/GUIDE_DEPLOIEMENT.md).

Fly.io reste disponible en fallback ([docs/deploiement-flyio.md](docs/deploiement-flyio.md)).

### Créer une release

Le projet utilise **GitHub Actions** : chaque tag `v*` déclenche le workflow `release.yml` qui lance les tests, produit un exécutable self-contained linux-x64 publié en **GitHub Release**, build l'image Docker, la pousse sur GHCR et déploie sur Northflank.

```bash
git tag v1.0.0
git push origin v1.0.0
```

Le pipeline génère des release notes auto-catégorisées par type de changement.

### Déployer une release (VPS Linux)

> Sur Northflank, le déploiement est automatique à la fin du workflow `release.yml` (ou via `Run workflow`). Les étapes ci-dessous concernent la cible VPS.

Depuis le PC local, à la racine du projet :

```bash
# Dernière release
./deployement/deploy.sh VOTRE_IP                    # Linux / macOS
.\deployement\deploy.ps1 -RemoteHost VOTRE_IP       # Windows

# Version spécifique
./deployement/deploy.sh VOTRE_IP root 1.2.0          # Linux / macOS
.\deployement\deploy.ps1 -RemoteHost VOTRE_IP -Version 1.2.0  # Windows
```

Le script télécharge la release depuis GitHub, la déploie sur le serveur et redémarre l'application.

### Rollback

Redéployer une version antérieure — toutes les releases sont conservées sur GitHub :

```bash
./deployement/deploy.sh VOTRE_IP root 1.0.0
```

### Vérification

```bash
systemctl status ecbatan-location
journalctl -u ecbatan-location -f
```

### Sauvegarde

Un cron sauvegarde la base SQLite chaque jour à 2h dans `/var/backups/ecbatan-location/` avec une rétention de 30 jours.

Voir [docs/GUIDE_DEPLOIEMENT.md](docs/GUIDE_DEPLOIEMENT.md) pour le guide complet (installation serveur, Nginx, HTTPS, backups).

## Licence

Projet privé — tous droits réservés.
