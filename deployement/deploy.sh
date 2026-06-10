#!/bin/bash
set -euo pipefail

# ============================================================
# Script de déploiement depuis une GitHub Release
# Usage : ./deployement/deploy.sh <IP_OU_HOST> [USER] [VERSION]
#
# Sans VERSION : déploie la dernière release
# Avec VERSION : déploie la version spécifiée (ex: 1.2.0)
# ============================================================

if [ $# -lt 1 ]; then
    echo "Usage : $0 <IP_OU_HOST> [USER] [VERSION]"
    echo "Exemple : $0 203.0.113.42"
    echo "Exemple : $0 planning.exemple.fr deploy 1.2.0"
    exit 1
fi

HOST="$1"
USER="${2:-root}"
APP_DIR="/var/www/ecbatan-location"
REPO="<votre-org>/ecbatan-location"

if [ $# -ge 3 ]; then
    VERSION="$3"
    echo "==> Version demandée : v${VERSION}"
else
    echo "==> Récupération de la dernière version..."
    VERSION=$(gh release view --repo "$REPO" --json tagName -q '.tagName' | sed 's/^v//')
    echo "    Dernière version : v${VERSION}"
fi

ARCHIVE="ecbatan-location-${VERSION}-linux-x64.tar.gz"
DOWNLOAD_URL="https://github.com/${REPO}/releases/download/v${VERSION}/${ARCHIVE}"

echo "==> Téléchargement de ${ARCHIVE}..."
TMPDIR=$(mktemp -d)
trap 'rm -rf "$TMPDIR"' EXIT
wget -q --show-progress -O "${TMPDIR}/${ARCHIVE}" "$DOWNLOAD_URL"

echo "==> Envoi sur le serveur..."
scp "${TMPDIR}/${ARCHIVE}" "${USER}@${HOST}:/tmp/${ARCHIVE}"

echo "==> Déploiement sur le serveur..."
ssh "${USER}@${HOST}" bash -s <<REMOTE
set -euo pipefail
sudo systemctl stop ecbatan-location
sudo tar -xzf /tmp/${ARCHIVE} -C ${APP_DIR}/
sudo chown -R planning:planning ${APP_DIR}
sudo systemctl start ecbatan-location
rm /tmp/${ARCHIVE}
REMOTE

echo "==> Vérification du statut..."
ssh "${USER}@${HOST}" "sleep 2 && systemctl is-active ecbatan-location"

echo ""
echo "==> Déploiement v${VERSION} terminé avec succès !"
