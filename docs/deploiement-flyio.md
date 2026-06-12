# Déploiement sur Fly.io — Ecbatan Location

## Prérequis

- Un compte GitHub avec le dépôt Ecbatan Location
- Un compte [Fly.io](https://fly.io) (inscription gratuite, carte bancaire requise pour validation d'identité)

## Installation initiale (une seule fois)

### 1. Installer le CLI Fly.io

**Windows (PowerShell) :**

```powershell
winget install flyctl
```

**Linux / macOS :**

```bash
curl -L https://fly.io/install.sh | sh
```

### 2. Se connecter

```bash
fly auth login
```

Un navigateur s'ouvre pour l'authentification.

### 3. Créer l'application sur Fly.io

Depuis la racine du projet :

```bash
fly launch --no-deploy
```

- Nom de l'app : `ecbatan-location` (ou un nom disponible de votre choix)
- Région : `cdg` (Paris)
- **Refuser** PostgreSQL et Redis quand proposé (on utilise SQLite)
- L'option `--no-deploy` évite un premier déploiement avant que le volume soit prêt

> Si le nom `ecbatan-location` est pris, choisir un autre nom et mettre à jour la ligne `app = '...'` dans `fly.toml`.

### 4. Créer le volume de stockage persistant

```bash
fly volumes create ecbatan_data --size 1 --region cdg
```

Cela crée un disque de 1 Go dans la région Paris. C'est là que le fichier SQLite sera stocké. Les données survivent aux redéploiements.

### 5. Premier déploiement

```bash
fly deploy
```

Fly.io va :
1. Builder l'image Docker à partir du `Dockerfile`
2. Pousser l'image vers son registre
3. Lancer la micro-VM avec le volume monté sur `/data`

### 6. Vérifier que tout fonctionne

```bash
# Voir le statut de l'app
fly status

# Ouvrir dans le navigateur
fly open

# Consulter les logs en temps réel
fly logs
```

## Configurer le déploiement automatique via GitHub Actions

Le workflow `release.yml` déploie automatiquement sur Fly.io quand un tag `v*` est poussé.

### 1. Générer un token de déploiement Fly.io

```bash
fly tokens create deploy -x 999999h
```

Copier le token affiché.

### 2. Ajouter le secret dans GitHub

1. Aller sur le dépôt GitHub > **Settings** > **Secrets and variables** > **Actions**
2. Cliquer **New repository secret**
3. Nom : `FLY_API_TOKEN`
4. Valeur : coller le token de l'étape précédente

### 3. Déclencher un déploiement

Créer un tag et le pousser :

```bash
git tag v1.0.0
git push origin v1.0.0
```

Le workflow va automatiquement : lancer les tests, créer la release GitHub, puis déployer sur Fly.io.

## Commandes utiles au quotidien

```bash
# Déployer manuellement
fly deploy

# Voir les logs
fly logs

# Se connecter en SSH dans la VM
fly ssh console

# Vérifier la base SQLite
fly ssh console -C "ls -la /data/"

# Redémarrer l'application
fly apps restart ecbatan-location

# Voir la consommation / facturation
fly billing
```

## Sauvegarde de la base de données

Le volume Fly.io est persistant mais pas sauvegardé automatiquement. Pour télécharger une copie locale de la base :

```bash
fly ssh sftp get /data/ecbatanlocation.db ./backup-ecbatanlocation.db
```

## Limites du tier gratuit

| Ressource | Limite |
|-----------|--------|
| VMs | Jusqu'à 3 machines shared-cpu-1x |
| RAM | 256 Mo par VM (512 Mo configuré ici) |
| Stockage | 3 Go de volumes persistants |
| Bande passante | 100 Go/mois |
| Facturation | Aucune si < 5$/mois |

Pour un usage par 4 propriétaires avec trafic faible, ces limites sont largement suffisantes.
