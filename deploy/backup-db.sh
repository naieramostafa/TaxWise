#!/usr/bin/env bash
set -euo pipefail

# PostgreSQL backup script for Streamline-Tax-And-Compliance
# Usage: ./deploy/backup-db.sh [output-dir]
# Add to cron: 0 2 * * * /path/to/deploy/backup-db.sh /var/backups

OUT_DIR="${1:-./backups}"
DB_NAME="streamline_tax"
DB_USER="postgres"
DB_HOST="${PGHOST:-localhost}"
DB_PORT="${PGPORT:-5432}"
PGPASSWORD="${PGPASSWORD:-postgres}"
export PGPASSWORD

mkdir -p "$OUT_DIR"
STAMP="$(date +%Y%m%d_%H%M%S)"
DUMP_FILE="$OUT_DIR/${DB_NAME}_${STAMP}.dump"

# Plain SQL dump (portable, restorable via psql)
pg_dump -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$DB_NAME" -F c -f "$DUMP_FILE"

# Compress
gzip "$DUMP_FILE"

# Keep only the last 14 backups
find "$OUT_DIR" -name "${DB_NAME}_*.dump.gz" -mtime +14 -delete

echo "Backup created: ${DUMP_FILE}.gz"
echo "Backups retained: $(find "$OUT_DIR" -name '*.dump.gz' | wc -l)"
