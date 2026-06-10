#!/bin/bash
set -euo pipefail

# ============================================================
# 02 - Installation de Nginx + certificat SSL Let's Encrypt
# À exécuter en root sur le VPS
# ============================================================

# >>> MODIFIER CES VALEURS <<<
DOMAIN="planning.exemple.fr"
EMAIL="syl.cesari@gmail.com"

echo "==> Installation de Nginx..."
apt install -y nginx

echo "==> Installation de Certbot..."
apt install -y certbot python3-certbot-nginx

echo "==> Copie de la configuration Nginx..."
cp /tmp/deployement/nginx/planning-location.conf /etc/nginx/sites-available/planning-location
ln -sf /etc/nginx/sites-available/planning-location /etc/nginx/sites-enabled/
rm -f /etc/nginx/sites-enabled/default

# Remplacer le domaine dans la config
sed -i "s/planning\.exemple\.fr/$DOMAIN/g" /etc/nginx/sites-available/planning-location

echo "==> Test de la configuration Nginx..."
nginx -t

echo "==> Démarrage de Nginx..."
systemctl enable nginx
systemctl restart nginx

echo "==> Obtention du certificat SSL..."
certbot --nginx -d "$DOMAIN" --non-interactive --agree-tos -m "$EMAIL" --redirect

echo "==> Activation du renouvellement automatique..."
systemctl enable certbot.timer
systemctl start certbot.timer

echo ""
echo "==> Nginx + SSL configurés pour $DOMAIN"
echo "    Le certificat se renouvellera automatiquement."
