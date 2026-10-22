#!/usr/bin/env bash
# Saves the whole database to ./backup/billio-YYYYMMDD-HHMMSS.dump (pg_dump custom format)
# and keeps the newest $KEEP files (KEEP=0 keeps all). The time is UTC, so names sort in time
# order across DST changes. Usage: scripts/db-backup.sh
set -euo pipefail

CONTAINER=${CONTAINER:-billio-db}
DB=${DB:-Data}
KEEP=${KEEP:-14}
BACKUP_DIR=${BACKUP_DIR:-backup}
BACKUP_SUFFIX=${BACKUP_SUFFIX:-}
NAME="billio-$(date -u +%Y%m%d-%H%M%S)$BACKUP_SUFFIX.dump"
# Written under a .partial name and renamed once checked, so an interrupted backup is never
# listed as a backup
PART="$BACKUP_DIR/$NAME.partial"

mkdir -p "$BACKUP_DIR"

docker exec "$CONTAINER" pg_dump -U postgres -d "$DB" -Fc > "$PART" \
  || { rm -f "$PART"; echo "Backup failed" >&2; exit 1; }

# Check: the archive must be readable and hold table data. The list is read in full first:
# piping it into grep -q can kill docker exec with SIGPIPE, which pipefail reports as a failure.
contents=$(docker exec -i "$CONTAINER" pg_restore --list < "$PART") && grep -q "TABLE DATA" <<< "$contents" \
  || { rm -f "$PART"; echo "Backup check failed" >&2; exit 1; }
mv "$PART" "$BACKUP_DIR/$NAME"

# Names hold the timestamp, so sort by name, not by file time (a copied file gets a new one).
# Only regular backups rotate; -before-restore files stay until deleted by hand.
if [ "$KEEP" -gt 0 ]; then
  ls -1 "$BACKUP_DIR"/billio-????????-??????.dump | sort -r | tail -n +$((KEEP + 1)) | xargs -r rm --
fi

echo "Saved $BACKUP_DIR/$NAME ($(du -h "$BACKUP_DIR/$NAME" | cut -f1))"
