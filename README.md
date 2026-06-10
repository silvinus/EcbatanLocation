# Ecbatan Location

Application web de gestion d'un planning de location saisonnière pour une maison de vacances partagée entre 4 copropriétaires (Léa, Sarah, Jean, Christophe).

Le planning est consultable publiquement en lecture seule et éditable par les propriétaires authentifiés.

## Stack technique

| Couche | Technologie |
|--------|------------|
| Frontend | Blazor Server (.NET 10) |
| Backend | ASP.NET Core 10 |
| Authentification | ASP.NET Identity |
| Base de données | SQLite (via Entity Framework Core) |
| Architecture | DDD + CQRS (médiateur interne) |
| Tests | xUnit + FluentAssertions |
| Déploiement | Standalone sur VPS Linux (Ubuntu 24.04) |

## Fonctionnalités

### Planning de location

- **Vue mensuelle** : studios en lignes, jours en colonnes, code couleur par statut
- **Vues alternatives** : semaine et liste
- **Filtres** : par mois, studio, statut, propriétaire
- **KPIs d'occupation** : taux d'occupation, places occupées par jour

### Gestion des réservations

- Création, modification et suivi des réservations par les propriétaires
- Workflow de validation : Demande (orange) → Acceptée (bleu) → Confirmée (vert)
- Contrôle des chevauchements (un studio est libre ou occupé, pas de location partielle)
- Vérification de capacité (adultes + enfants ≤ capacité du studio)
- Gestion des studios dépendants (certains studios ne sont louables qu'en complément d'un autre)

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
| Propriétaire | Création, modification, changement de statut des réservations |
| Admin | Gestion des studios, tarifs et comptes propriétaires |

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

L'application se déploie sur un VPS Linux (Ubuntu 24.04 LTS) avec Nginx en reverse proxy et un certificat SSL Let's Encrypt.

### Première installation

```bash
# Copier les scripts sur le serveur
scp -r deployement/ root@VOTRE_IP:/tmp/deployement/

# Sur le serveur : installation initiale
ssh root@VOTRE_IP
chmod +x /tmp/deployement/*.sh
/tmp/deployement/01-setup-server.sh

# Configurer le domaine dans les fichiers Nginx puis installer
/tmp/deployement/02-install-nginx.sh
```

### Déploiements suivants

Depuis le PC local, à la racine du projet :

```bash
# Linux / macOS
./deployement/deploy.sh VOTRE_IP

# Windows (PowerShell)
.\deployement\deploy.ps1 VOTRE_IP
```

Le script compile l'application, la publie et la déploie sur le serveur.

### Vérification

```bash
systemctl status ecbatan-location
journalctl -u ecbatan-location -f
```

### Sauvegarde

Un cron sauvegarde la base SQLite chaque jour à 2h dans `/var/backups/ecbatan-location/` avec une rétention de 30 jours.

## Licence

Projet privé — tous droits réservés.
