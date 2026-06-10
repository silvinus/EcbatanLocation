# Déploiement Ecbatan Location

## Prérequis

- VPS Linux Ubuntu 24.04 LTS (Hetzner CX12 recommandé)
- Nom de domaine pointant vers l'IP du VPS
- .NET 10 SDK installé localement pour la compilation

## Étapes de déploiement

### 1. Première installation du serveur

```bash
# Copier tout le dossier deployement sur le serveur
scp -r deployement/ root@VOTRE_IP:/tmp/deployement/

# Se connecter au serveur
ssh root@VOTRE_IP

# Rendre les scripts exécutables
chmod +x /tmp/deployement/*.sh

# Lancer l'installation initiale (en root)
/tmp/deployement/01-setup-server.sh
```

### 2. Configurer le domaine

Éditer les fichiers avant de lancer l'installation :
- `nginx/ecbatan-location.conf` : remplacer `planning.exemple.fr` par votre domaine
- `02-install-nginx.sh` : remplacer le domaine et l'email pour Let's Encrypt

### 3. Installer Nginx + SSL

```bash
/tmp/deployement/02-install-nginx.sh
```

### 4. Compiler et déployer l'application (depuis le PC local)

```bash
# Depuis la racine du projet
./deployement/deploy.sh VOTRE_IP
```

### 5. Vérifier

```bash
# Sur le serveur
systemctl status ecbatan-location
journalctl -u ecbatan-location -f
```

## Déploiements suivants

Depuis le PC local, relancer simplement :

```bash
./deployement/deploy.sh VOTRE_IP
```

## Backup

Un cron sauvegarde la base SQLite chaque jour à 2h dans `/var/backups/ecbatan-location/`.
Rétention : 30 jours.
