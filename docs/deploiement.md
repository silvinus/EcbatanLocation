---
layout: default
title: Deploiement
nav_order: 6
---

# Guide de deploiement
{: .fs-8 }

Installation serveur, deploiement et maintenance.
{: .fs-5 .fw-300 }

---

## Prerequis

- Compte GitHub avec le repo du projet
- Serveur VPS Linux (Ubuntu 22.04+ recommande)
- Acces SSH au serveur

---

## Creer une release

Le projet utilise **GitHub Actions** pour builder automatiquement un artefact deployable a chaque tag de version.

### Creer un tag

```bash
git tag v1.0.0
git push origin v1.0.0
```

GitHub Actions execute automatiquement :
1. Restauration des dependances
2. Lancement des tests — si un test echoue, la release est bloquee
3. Build d'un executable **self-contained linux-x64** (pas besoin de .NET sur le serveur)
4. Publication d'une **GitHub Release** avec l'archive

### Convention de versioning

Utiliser le [versioning semantique](https://semver.org/lang/fr/) :
- `v1.0.0` : premiere release stable
- `v1.1.0` : ajout de fonctionnalites
- `v1.1.1` : correction de bug
- `v2.0.0` : changement majeur

---

## Premiere installation du serveur

### Creer les repertoires et l'utilisateur

```bash
sudo useradd -r -s /bin/false planning
sudo mkdir -p /var/www/ecbatan-location
sudo mkdir -p /var/lib/ecbatan-location
sudo chown -R planning:planning /var/www/ecbatan-location
sudo chown -R planning:planning /var/lib/ecbatan-location
```

### Configuration de production

Creer `/var/www/ecbatan-location/appsettings.Production.json` :

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

{: .warning }
Ne jamais commiter ce fichier dans le repository.

### Service systemd

Creer `/etc/systemd/system/ecbatan-location.service` :

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

### Nginx (reverse proxy)

Creer `/etc/nginx/sites-available/ecbatan-location` :

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

{: .note }
Le `Connection "upgrade"` est indispensable pour Blazor Server (SignalR/WebSocket).

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

---

## Deployer une release

### Option A — Script automatise (recommande)

Depuis le PC local, a la racine du projet :

```bash
# Linux / macOS
./deployement/deploy.sh <IP_OU_HOST> [USER]

# Windows (PowerShell)
.\deployement\deploy.ps1 -RemoteHost <IP_OU_HOST> [-User root]
```

Le script telecharge la derniere release GitHub, la deploie sur le serveur et redemarre l'application.

### Option B — Deploiement manuel

```bash
VERSION="1.0.0"
wget https://github.com/silvinus/EcbatanLocation/releases/download/v${VERSION}/ecbatan-location-${VERSION}-linux-x64.tar.gz
sudo systemctl stop ecbatan-location
sudo tar -xzf ecbatan-location-${VERSION}-linux-x64.tar.gz -C /var/www/ecbatan-location/
sudo chown -R planning:planning /var/www/ecbatan-location
sudo systemctl start ecbatan-location
```

### Verification post-deploiement

```bash
sudo systemctl status ecbatan-location
sudo journalctl -u ecbatan-location -f
```

1. Acceder a `https://planning.votredomaine.fr` — le planning s'affiche
2. Se connecter avec un compte proprietaire — mode proprietaire actif
3. Creer une reservation test — verifier la persistence

---

## Backup automatique SQLite

Script `/var/www/ecbatan-location/backup.sh` :

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

Crontab (`sudo crontab -e`) :

```cron
0 2 * * * /var/www/ecbatan-location/backup.sh
```

---

## Rollback

Redeployer une version anterieure — toutes les releases sont conservees sur GitHub :

```bash
./deployement/deploy.sh <IP_OU_HOST> root 1.0.0
```
