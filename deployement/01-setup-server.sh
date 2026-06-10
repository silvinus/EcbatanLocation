#!/bin/bash
set -euo pipefail

# ============================================================
# 01 - Configuration initiale du serveur Ubuntu 24.04
# À exécuter en root sur le VPS
# ============================================================

APP_USER="planning"
APP_DIR="/var/www/planning-location"
DATA_DIR="/var/www/planning-location/data"
BACKUP_DIR="/var/backups/planning-location"

echo "==> Mise à jour du système..."
apt update && apt upgrade -y

echo "==> Installation des paquets de base..."
apt install -y \
    curl \
    wget \
    unzip \
    ufw \
    fail2ban \
    logrotate

# --- Runtime .NET 10 ---
echo "==> Installation du runtime .NET 10..."
wget https://dot.net/v1/dotnet-install.sh -O /tmp/dotnet-install.sh
chmod +x /tmp/dotnet-install.sh
/tmp/dotnet-install.sh --runtime aspnetcore --version latest --install-dir /usr/share/dotnet
ln -sf /usr/share/dotnet/dotnet /usr/bin/dotnet
dotnet --info

# --- Utilisateur applicatif ---
echo "==> Création de l'utilisateur applicatif..."
if ! id "$APP_USER" &>/dev/null; then
    useradd -r -s /usr/sbin/nologin -d "$APP_DIR" "$APP_USER"
fi

# --- Répertoires ---
echo "==> Création des répertoires..."
mkdir -p "$APP_DIR"
mkdir -p "$DATA_DIR"
mkdir -p "$BACKUP_DIR"
chown -R "$APP_USER:$APP_USER" "$APP_DIR"
chown -R "$APP_USER:$APP_USER" "$BACKUP_DIR"

# --- Firewall ---
echo "==> Configuration du firewall..."
ufw default deny incoming
ufw default allow outgoing
ufw allow ssh
ufw allow 80/tcp
ufw allow 443/tcp
ufw --force enable
ufw status

# --- Fail2ban ---
echo "==> Configuration de fail2ban..."
cat > /etc/fail2ban/jail.local << 'EOF'
[DEFAULT]
bantime = 3600
findtime = 600
maxretry = 5

[sshd]
enabled = true
port = ssh
logpath = /var/log/auth.log
EOF

systemctl enable fail2ban
systemctl restart fail2ban

# --- Service systemd ---
echo "==> Installation du service systemd..."
cp /tmp/deployement/systemd/planning-location.service /etc/systemd/system/
systemctl daemon-reload
systemctl enable planning-location

# --- Backup cron ---
echo "==> Installation du script de backup..."
cp /tmp/deployement/backup.sh /usr/local/bin/backup-planning.sh
chmod +x /usr/local/bin/backup-planning.sh

cat > /etc/cron.d/planning-backup << 'EOF'
0 2 * * * root /usr/local/bin/backup-planning.sh
EOF

# --- Logrotate ---
echo "==> Configuration du logrotate..."
cat > /etc/logrotate.d/planning-location << 'EOF'
/var/www/planning-location/logs/*.log {
    daily
    missingok
    rotate 14
    compress
    delaycompress
    notifempty
    create 0644 planning planning
    sharedscripts
    postrotate
        systemctl restart planning-location
    endscript
}
EOF

echo ""
echo "==> Installation serveur terminée."
echo "    Prochaine étape : exécuter 02-install-nginx.sh"
