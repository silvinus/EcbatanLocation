#!/bin/bash
set -euo pipefail

# ============================================================
# Backup quotidien de la base SQLite
# Exécuté par cron chaque jour à 2h
# Rétention : 30 jours
# ============================================================

DB_PATH="/var/www/planning-location/data/planning.db"
BACKUP_DIR="/var/backups/planning-location"
DATE=$(date +%Y-%m-%d_%H%M)
RETENTION_DAYS=30

if [ ! -f "$DB_PATH" ]; then
    echo "Base de données non trouvée : $DB_PATH"
    exit 1
fi

# Backup via sqlite3 .backup (safe même si l'app écrit)
sqlite3 "$DB_PATH" ".backup '${BACKUP_DIR}/planning_${DATE}.db'"

# Compression
gzip "${BACKUP_DIR}/planning_${DATE}.db"

# Nettoyage des vieux backups
find "$BACKUP_DIR" -name "planning_*.db.gz" -mtime +$RETENTION_DAYS -delete

echo "Backup terminé : planning_${DATE}.db.gz"
