# Guide de déploiement - Ecbatan Location

## Prérequis

- .NET 10 SDK installé sur la machine de build
- Serveur VPS Linux (Ubuntu 22.04+ recommandé)
- Accès SSH au serveur

## 1. Publication de l'application

### Build standalone

```bash
dotnet publish src/EcbatanLocation.Web -c Release -o ./publish --self-contained -r linux-x64
```

### Build framework-dependent (nécessite .NET 10 runtime sur le serveur)

```bash
dotnet publish src/EcbatanLocation.Web -c Release -o ./publish
```

## 2. Configuration de production

Créer le fichier `appsettings.Production.json` dans le dossier de publication :

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=/var/lib/EcbatanLocation/planning.db"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

> **Important** : Ne jamais commiter ce fichier. Modifier les chemins et secrets selon votre environnement.

## 3. Transfert sur le serveur

```bash
rsync -avz ./publish/ user@serveur:/opt/EcbatanLocation/
```

## 4. Préparation du serveur

### Créer les répertoires

```bash
sudo mkdir -p /opt/EcbatanLocation
sudo mkdir -p /var/lib/EcbatanLocation
sudo chown -R www-data:www-data /var/lib/EcbatanLocation
```

### Rendre l'exécutable fonctionnel (build standalone)

```bash
sudo chmod +x /opt/EcbatanLocation/EcbatanLocation.Web
```

## 5. Service systemd

Créer le fichier `/etc/systemd/system/EcbatanLocation.service` :

```ini
[Unit]
Description=Ecbatan Location - Application de gestion
After=network.target

[Service]
WorkingDirectory=/opt/EcbatanLocation
ExecStart=/opt/EcbatanLocation/EcbatanLocation.Web
Restart=always
RestartSec=10
SyslogIdentifier=EcbatanLocation
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://localhost:5000

[Install]
WantedBy=multi-user.target
```

### Activer et démarrer le service

```bash
sudo systemctl daemon-reload
sudo systemctl enable EcbatanLocation
sudo systemctl start EcbatanLocation
sudo systemctl status EcbatanLocation
```

### Consulter les logs

```bash
sudo journalctl -u EcbatanLocation -f
```

## 6. Reverse proxy Nginx

Installer Nginx et créer `/etc/nginx/sites-available/EcbatanLocation` :

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
sudo ln -s /etc/nginx/sites-available/EcbatanLocation /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl reload nginx
```

## 7. Certificat HTTPS (Let's Encrypt)

```bash
sudo apt install certbot python3-certbot-nginx
sudo certbot --nginx -d planning.votredomaine.fr
```

Certbot configure automatiquement la redirection HTTP → HTTPS et le renouvellement automatique.

## 8. Backup automatique SQLite

Créer le script `/opt/EcbatanLocation/backup.sh` :

```bash
#!/bin/bash
BACKUP_DIR="/var/backups/EcbatanLocation"
DB_PATH="/var/lib/EcbatanLocation/planning.db"
DATE=$(date +%Y%m%d_%H%M%S)

mkdir -p "$BACKUP_DIR"
sqlite3 "$DB_PATH" ".backup '$BACKUP_DIR/planning_$DATE.db'"

# Conserver les 30 derniers backups
ls -t "$BACKUP_DIR"/planning_*.db | tail -n +31 | xargs -r rm
```

```bash
sudo chmod +x /opt/EcbatanLocation/backup.sh
```

Ajouter au crontab (`sudo crontab -e`) :

```cron
0 2 * * * /opt/EcbatanLocation/backup.sh
```

## 9. Mise à jour de l'application

```bash
# Sur la machine de build
dotnet publish src/EcbatanLocation.Web -c Release -o ./publish --self-contained -r linux-x64

# Transfert
rsync -avz ./publish/ user@serveur:/opt/EcbatanLocation/

# Sur le serveur
sudo systemctl restart EcbatanLocation
```

## 10. Vérifications post-déploiement

1. Accéder à `https://planning.votredomaine.fr` → le planning s'affiche
2. Se connecter avec un compte propriétaire → mode propriétaire actif
3. Créer une réservation test → vérifier la persistence (rafraîchir la page)
4. Vérifier les headers de sécurité : `curl -I https://planning.votredomaine.fr`
5. Vérifier les logs : `sudo journalctl -u EcbatanLocation --since "5 minutes ago"`
