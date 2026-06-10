#!/bin/bash
set -euo pipefail

# ============================================================
# Script de déploiement (à exécuter depuis le PC local)
# Usage : ./deployement/deploy.sh <IP_OU_HOST> [USER]
# ============================================================

if [ $# -lt 1 ]; then
    echo "Usage : $0 <IP_OU_HOST> [USER]"
    echo "Exemple : $0 203.0.113.42"
    echo "Exemple : $0 planning.exemple.fr deploy"
    exit 1
fi

HOST="$1"
USER="${2:-root}"
APP_DIR="/var/www/planning-location"
PUBLISH_DIR="./publish"
PROJECT="src/PlanningLocation.Web/PlanningLocation.Web.csproj"

echo "==> Compilation en Release..."
dotnet publish "$PROJECT" \
    -c Release \
    -o "$PUBLISH_DIR" \
    --self-contained false \
    -r linux-x64

echo "==> Copie de la config de production..."
cp deployement/appsettings.Production.json "$PUBLISH_DIR/"

echo "==> Envoi des fichiers sur le serveur..."
rsync -avz --delete \
    --exclude 'data/' \
    --exclude 'logs/' \
    "$PUBLISH_DIR/" "${USER}@${HOST}:${APP_DIR}/"

echo "==> Correction des permissions..."
ssh "${USER}@${HOST}" "chown -R planning:planning ${APP_DIR}"

echo "==> Redémarrage de l'application..."
ssh "${USER}@${HOST}" "systemctl restart planning-location"

echo "==> Vérification du statut..."
ssh "${USER}@${HOST}" "sleep 2 && systemctl is-active planning-location"

echo ""
echo "==> Déploiement terminé avec succès !"

# Nettoyage local
rm -rf "$PUBLISH_DIR"
