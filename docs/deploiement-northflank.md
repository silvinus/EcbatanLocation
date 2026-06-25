---
layout: default
title: Deploiement Northflank
nav_order: 6
---

# Déploiement sur Northflank — Ecbatan Location

> Cible de déploiement **par défaut** (image Docker via GitHub Container Registry).

## Prérequis

- Un compte GitHub avec le dépôt Ecbatan Location
- Un compte [Northflank](https://northflank.com) (inscription gratuite, pas de carte bancaire requise)
- Un GitHub Personal Access Token (classic) avec le scope `read:packages`

## Free tier Northflank (Developer Sandbox)

| Ressource | Inclus |
|-----------|--------|
| Services | 2 |
| CPU | 1 vCPU (partagé) |
| RAM | 1 GB |
| Volume persistant | 0.5 GB |
| Build | Depuis GitHub |
| Cold starts | Non |

## Installation initiale (une seule fois)

### 1. Créer le projet Northflank

1. Se connecter sur [app.northflank.com](https://app.northflank.com)
2. Cliquer **Create project** > Nom : `ecbatan-location`
3. Noter le **Project ID** (visible dans l'URL ou dans Project Settings)

### 2. Enregistrer les credentials GHCR

L'image Docker est stockée sur GitHub Container Registry. Northflank a besoin d'un accès en lecture.

1. Aller dans **Account** > **Registries** > **Add credentials**
2. Remplir :
   - **Name** : `github` (ou un nom de votre choix)
   - **Registry URL** : `ghcr.io`
   - **Username** : votre username GitHub
   - **Password** : un GitHub Personal Access Token (classic) avec le scope `read:packages`
3. Enregistrer et noter le **Credentials ID** affiché

### 3. Créer le service

1. Dans le projet, cliquer **Add service** > **Deployment service**
2. Sélectionner **External image** sous Deployment
3. Laisser le champ image vide pour l'instant (le premier déploiement via GitHub Actions le remplira)
4. Configurer :
   - **Port** : `8080` (HTTP)
   - **Resources** : Free tier (0.5 vCPU, 512 MB RAM ou le maximum autorisé)
5. Créer le service et noter le **Service ID** (visible dans l'URL ou dans Service Settings)

### 4. Créer le volume persistant

1. Dans le service, aller dans **Storage** > **Add volume**
2. Configurer :
   - **Mount path** : `/data`
   - **Size** : `0.5 GB` (inclus dans le free tier)
3. Enregistrer

### 5. Configurer les variables d'environnement

Dans le service, aller dans **Environment** > **Runtime variables** :

| Variable | Valeur |
|----------|--------|
| `ASPNETCORE_URLS` | `http://+:8080` |
| `ConnectionStrings__DefaultConnection` | `Data Source=/data/ecbatanlocation.db` |

### 6. Générer un token API Northflank

1. Aller dans **Account** > **API** > **Create API token**
2. Donner les permissions de déploiement
3. Copier le token généré

### 7. Ajouter les secrets dans GitHub

Depuis un terminal avec `gh` configuré :

```bash
gh secret set NORTHFLANK_API_KEY
gh secret set NORTHFLANK_PROJECT_ID
gh secret set NORTHFLANK_SERVICE_ID
gh secret set NORTHFLANK_CREDENTIALS_ID
```

Chaque commande demande de coller la valeur correspondante.

Ou manuellement : dépôt GitHub > **Settings** > **Secrets and variables** > **Actions** > **New repository secret**.

| Secret | Valeur |
|--------|--------|
| `NORTHFLANK_API_KEY` | Token API Northflank (étape 6) |
| `NORTHFLANK_PROJECT_ID` | ID du projet (étape 1) |
| `NORTHFLANK_SERVICE_ID` | ID du service (étape 3) |
| `NORTHFLANK_CREDENTIALS_ID` | ID des credentials GHCR (étape 2) |

## Déclencher un déploiement

### Déploiement automatique (tag)

Créer un tag et le pousser :

```bash
git tag v1.0.0
git push origin v1.0.0
```

Le workflow `release.yml` va automatiquement : lancer les tests, créer la release GitHub, builder l'image Docker, la pousser sur GHCR, puis déployer sur Northflank.

### Déploiement manuel (workflow_dispatch)

1. Aller sur GitHub > **Actions** > **Build & Release**
2. Cliquer **Run workflow**
3. Choisir la cible : `northflank` (défaut) ou `fly` (fallback)
4. Cliquer **Run workflow**

## Pipeline de déploiement

```
Tag v* poussé
  → Tests (.NET)
  → Publish (archive linux-x64)
  → Release GitHub
  → Build image Docker
  → Push sur ghcr.io
  → Northflank pull l'image et redéploie le service
```

## Commandes utiles

### Northflank CLI (optionnel)

```bash
# Installer
npm install -g @northflank/cli

# Se connecter
northflank login

# Voir les logs
northflank logs --project ecbatan-location --service <service-name>

# Lister les services
northflank services list --project ecbatan-location
```

### Sauvegarde de la base SQLite

Depuis la console Northflank (service > **Shell**) :

```bash
cat /data/ecbatanlocation.db > /tmp/backup.db
```

Ou télécharger via la CLI Northflank si disponible.

## Limites du free tier

| Ressource | Limite |
|-----------|--------|
| Services | 2 |
| CPU | 1 vCPU partagé |
| RAM | 1 GB |
| Volume persistant | 0.5 GB |
| Jobs/Cron | 2 |
| Projets | 2 |

Pour un usage par 4 propriétaires avec trafic faible, ces limites sont largement suffisantes.
