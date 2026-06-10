# Guide de déploiement - Ecbatan Location

## Prérequis

- Compte GitHub avec le repo du projet
- Serveur VPS Linux (Ubuntu 22.04+ recommandé)
- Accès SSH au serveur

## 1. Créer une release

Le projet utilise **GitHub Actions** pour builder automatiquement un artifact déployable à chaque tag de version.

### Créer un tag et déclencher le build

```bash
git tag v1.0.0
git push origin v1.0.0
```

GitHub Actions exécute automatiquement :
1. Restauration des dépendances
2. Lancement des tests — si un test échoue, la release est bloquée
3. Build d'un exécutable **self-contained linux-x64** (single file, pas besoin de .NET sur le serveur)
4. Publication d'une **GitHub Release** avec l'archive téléchargeable

### Suivre le build

Le pipeline est visible dans l'onglet **Actions** du repo GitHub. La release apparaît dans l'onglet **Releases** une fois le build terminé.

### Convention de versioning

Utiliser le [versioning sémantique](https://semver.org/lang/fr/) :
- `v1.0.0` → première release stable
- `v1.1.0` → ajout de fonctionnalités
- `v1.1.1` → correction de bug
- `v2.0.0` → changement majeur (breaking changes)

## 2. Configuration de production

Créer le fichier `appsettings.Production.json` sur le serveur dans `/var/www/ecbatan-location/` :

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=/var/lib/ecbatan-location/planning.db"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

> **Important** : Ce fichier contient la configuration spécifique au serveur. Ne jamais le commiter dans le repo.

## 3. Première installation du serveur

### Créer les répertoires et l'utilisateur

```bash
sudo useradd -r -s /bin/false planning
sudo mkdir -p /var/www/ecbatan-location
sudo mkdir -p /var/lib/ecbatan-location
sudo chown -R planning:planning /var/www/ecbatan-location
sudo chown -R planning:planning /var/lib/ecbatan-location
```

### Installer le service systemd

Créer `/etc/systemd/system/ecbatan-location.service` :

```ini
[Unit]
Description=Ecbatan Location - Application de gestion
After=network.target

[Service]
WorkingDirectory=/var/www/ecbatan-location
ExecStart=/var/www/ecbatan-location/EcbatanLocation.Web
Restart=always
RestartSec=10
SyslogIdentifier=ecbatan-location
User=planning
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://localhost:5000

[Install]
WantedBy=multi-user.target
```

```bash
sudo systemctl daemon-reload
sudo systemctl enable ecbatan-location
```

### Configurer Nginx (reverse proxy)

Installer Nginx et créer `/etc/nginx/sites-available/ecbatan-location` :

```nginx
server {
    listen 80;
    server_name planning.votredomaine.fr;

    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
    }
}
```

> **Note** : Le `Connection "upgrade"` est indispensable pour Blazor Server (SignalR/WebSocket).

```bash
sudo ln -s /etc/nginx/sites-available/ecbatan-location /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl reload nginx
```

### Certificat HTTPS (Let's Encrypt)

```bash
sudo apt install certbot python3-certbot-nginx
sudo certbot --nginx -d planning.votredomaine.fr
```

Certbot configure automatiquement la redirection HTTP → HTTPS et le renouvellement.

## 4. Déployer une release

### Option A — Script automatisé (recommandé)

Depuis le PC local, à la racine du projet :

```bash
# Linux / macOS
./deployement/deploy.sh <IP_OU_HOST> [USER]

# Windows (PowerShell)
.\deployement\deploy.ps1 -RemoteHost <IP_OU_HOST> [-User root]
```

Le script télécharge la dernière release GitHub, la déploie sur le serveur et redémarre l'application.

### Option B — Déploiement manuel

Sur le serveur :

```bash
# Télécharger la release
VERSION="1.0.0"
wget https://github.com/<votre-org>/ecbatan-location/releases/download/v${VERSION}/ecbatan-location-${VERSION}-linux-x64.tar.gz

# Arrêter l'application
sudo systemctl stop ecbatan-location

# Déployer
sudo tar -xzf ecbatan-location-${VERSION}-linux-x64.tar.gz -C /var/www/ecbatan-location/
sudo chown -R planning:planning /var/www/ecbatan-location

# Redémarrer
sudo systemctl start ecbatan-location
```

### Vérification post-déploiement

```bash
# Statut du service
sudo systemctl status ecbatan-location

# Logs en temps réel
sudo journalctl -u ecbatan-location -f
```

1. Accéder à `https://planning.votredomaine.fr` → le planning s'affiche
2. Se connecter avec un compte propriétaire → mode propriétaire actif
3. Créer une réservation test → vérifier la persistence (rafraîchir la page)

## 5. Backup automatique SQLite

Créer le script `/var/www/ecbatan-location/backup.sh` :

```bash
#!/bin/bash
BACKUP_DIR="/var/backups/ecbatan-location"
DB_PATH="/var/lib/ecbatan-location/planning.db"
DATE=$(date +%Y%m%d_%H%M%S)

mkdir -p "$BACKUP_DIR"
sqlite3 "$DB_PATH" ".backup '$BACKUP_DIR/planning_$DATE.db'"

# Conserver les 30 derniers backups
ls -t "$BACKUP_DIR"/planning_*.db | tail -n +31 | xargs -r rm
```

```bash
sudo chmod +x /var/www/ecbatan-location/backup.sh
```

Ajouter au crontab (`sudo crontab -e`) :

```cron
0 2 * * * /var/www/ecbatan-location/backup.sh
```

## 6. Rollback

En cas de problème après un déploiement, redéployer la version précédente :

```bash
VERSION="1.0.0"  # version stable précédente
sudo systemctl stop ecbatan-location
wget https://github.com/<votre-org>/ecbatan-location/releases/download/v${VERSION}/ecbatan-location-${VERSION}-linux-x64.tar.gz
sudo tar -xzf ecbatan-location-${VERSION}-linux-x64.tar.gz -C /var/www/ecbatan-location/
sudo chown -R planning:planning /var/www/ecbatan-location
sudo systemctl start ecbatan-location
```

Toutes les releases sont conservées sur GitHub et téléchargeables à tout moment.
