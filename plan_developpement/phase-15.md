# Phase 15 : Migration déploiement Fly.io → Northflank

**Objectif** : Migrer l'hébergement de Fly.io (free tier supprimé en 2024, devenu payant) vers Northflank (free tier Developer Sandbox), tout en conservant le contrôle du déploiement via tags Git dans GitHub Actions.

## Contexte

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

## Stratégie de déploiement

**Choix retenu** : GitHub Actions avec API Northflank (et non le CI/CD intégré Northflank).

**Raison** : conserver le déclenchement par tag `v*` dans le workflow `release.yml`. Le CI/CD intégré Northflank se déclenche sur chaque push, sans contrôle fin sur le moment du déploiement. Avec GitHub Actions, on garde la maîtrise : seul un tag déclenche le pipeline tests → release → deploy.

## Tâches

### 1. Configuration Northflank (manuelle, une seule fois)

- Créer un compte Northflank et un projet `ecbatan-location`.
- Connecter le dépôt GitHub au projet Northflank.
- Créer un service « Combined » (build + run) pointant vers le `Dockerfile`.
- Créer un volume persistant de 0.5 GB monté sur `/data`.
- Configurer les variables d'environnement : `ASPNETCORE_URLS=http://+:8080`, `ConnectionStrings__DefaultConnection=Data Source=/data/ecbatanlocation.db`.
- Récupérer les identifiants API : **API token**, **Project ID**, **Service ID**.

### 2. Secrets GitHub

- Ajouter les secrets dans le dépôt GitHub (Settings > Secrets > Actions) :
  - `NORTHFLANK_API_TOKEN` : token API Northflank
  - `NORTHFLANK_PROJECT_ID` : ID du projet
  - `NORTHFLANK_SERVICE_ID` : ID du service

### 3. Workflow GitHub Actions — déploiement conditionnel

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

### 4. Job `deploy-northflank` — détail

- Utilise l'API REST Northflank pour déclencher un pipeline de déploiement :
  1. Créer un build via `POST /v1/projects/{projectId}/services/{serviceId}/builds` avec la référence Git (tag).
  2. Attendre la fin du build (polling status ou webhook).
- Alternative : utiliser la CLI Northflank (`npx @northflank/cli`) si disponible.

### 5. Documentation

- Créer `docs/deploiement-northflank.md` sur le modèle de `docs/deploiement-flyio.md` :
  - Prérequis (compte Northflank, CLI optionnel)
  - Installation initiale (création projet, service, volume)
  - Configuration GitHub Actions (secrets)
  - Commandes utiles (logs, backup SQLite)
  - Limites du free tier

### 6. Nettoyage (après validation)

- Retirer `fly.toml` de la branche principale (conserver dans l'historique Git).
- Mettre à jour le `README.md` si nécessaire.
- Supprimer le secret `FLY_API_TOKEN` du dépôt GitHub (après confirmation que Northflank fonctionne).

## Dockerfile

Le `Dockerfile` existant est **compatible tel quel** avec Northflank. Aucune modification nécessaire :
- Port 8080 exposé ✅
- Variable `ConnectionStrings__DefaultConnection` pointant vers `/data/` ✅
- Image multi-stage (SDK build + runtime) ✅

**Livrable** : Déploiement automatique sur Northflank via tag Git, job Fly.io conservé en fallback, documentation de déploiement.
