#!/usr/bin/env bash
# Saves the whole database to ./backup/billio-YYYYMMDD-HHMMSS.dump (pg_dump custom format)
# and keeps the newest $KEEP files. Usage: scripts/db-backup.sh
set -euo pipefail

CONTAINER=${CONTAINER:-billio-db}
DB=${DB:-Data}
KEEP=${KEEP:-14}
BACKUP_DIR=${BACKUP_DIR:-backup}
NAME="billio-$(date +%Y%m%d-%H%M%S).dump"

mkdir -p "$BACKUP_DIR"

docker exec "$CONTAINER" pg_dump -U postgres -d "$DB" -Fc > "$BACKUP_DIR/$NAME" \
  || { rm -f "$BACKUP_DIR/$NAME"; echo "Backup failed" >&2; exit 1; }

# Check: the archive must be readable and hold table data
docker exec -i "$CONTAINER" pg_restore --list < "$BACKUP_DIR/$NAME" | grep -q "TABLE DATA" \
  || { echo "Backup check failed: $BACKUP_DIR/$NAME" >&2; exit 1; }

ls -1t "$BACKUP_DIR"/billio-*.dump | tail -n +$((KEEP + 1)) | xargs -r rm --

echo "Saved $BACKUP_DIR/$NAME ($(du -h "$BACKUP_DIR/$NAME" | cut -f1))"
