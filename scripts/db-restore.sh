#!/usr/bin/env bash
# Restores a backup made by db-backup.sh from ./backup.
# Usage: scripts/db-restore.sh [<file>] [--yes] [--target <db>]
#   <file>         a file name in ./backup or a path; without it, lists the backups in ./backup
#                  and asks which one to restore (with --yes, takes the newest)
#   default        restores into Data: asks for confirmation, stops webapi, restores into a
#                  temp database and swaps it in only if the restore succeeds
#   --target <db>  restores into a scratch database and leaves Data and webapi alone
#   --yes          skips the questions
set -euo pipefail

CONTAINER=${CONTAINER:-billio-db}
WEBAPI=${WEBAPI:-billio-webapi}
DB=${DB:-Data}
BACKUP_DIR=${BACKUP_DIR:-backup}
TABLES="users sellers customers items invoices"

usage() { echo "Usage: $0 [<file>] [--yes] [--target <db>]" >&2; exit 1; }

FILE=""
TARGET="$DB"
YES=false
while [ $# -gt 0 ]; do
  case "$1" in
    --yes) YES=true; shift ;;
    --target) [ $# -ge 2 ] || usage; TARGET="$2"; shift 2 ;;
    -*) usage ;;
    *) [ -z "$FILE" ] || usage; FILE="$1"; shift ;;
  esac
done
if [ -z "$FILE" ]; then
  mapfile -t BACKUPS < <(ls -1t "$BACKUP_DIR"/billio-*.dump 2>/dev/null)
  [ ${#BACKUPS[@]} -gt 0 ] || { echo "No backups in $BACKUP_DIR" >&2; exit 1; }
  if [ "$YES" = true ]; then
    FILE="${BACKUPS[0]}"
  else
    echo "Backups in $BACKUP_DIR (newest first):"
    for i in "${!BACKUPS[@]}"; do
      printf '%3d) %s  %s\n' $((i + 1)) "$(basename "${BACKUPS[$i]}")" "$(du -h "${BACKUPS[$i]}" | cut -f1)"
    done
    read -r -p "Pick a backup to restore (1-${#BACKUPS[@]}): " pick
    [[ "$pick" =~ ^[0-9]+$ ]] && [ "$pick" -ge 1 ] && [ "$pick" -le ${#BACKUPS[@]} ] \
      || { echo "Cancelled." >&2; exit 1; }
    FILE="${BACKUPS[$((pick - 1))]}"
  fi
elif [ ! -f "$FILE" ] && [ -f "$BACKUP_DIR/$FILE" ]; then
  FILE="$BACKUP_DIR/$FILE"
fi
[ -f "$FILE" ] || { echo "File not found: $FILE" >&2; exit 1; }

psql_admin() { docker exec "$CONTAINER" psql -U postgres -d postgres -v ON_ERROR_STOP=1 -q -c "$1"; }

# Drops and recreates $1, then restores $FILE into it
restore_into() {
  psql_admin "DROP DATABASE IF EXISTS \"$1\" WITH (FORCE);"
  psql_admin "CREATE DATABASE \"$1\" TEMPLATE template0;"
  docker exec -i "$CONTAINER" pg_restore -U postgres -d "$1" --no-owner --exit-on-error < "$FILE"
}

print_counts() {
  for table in $TABLES; do
    printf '%-10s %s\n' "$table" "$(docker exec "$CONTAINER" psql -U postgres -d "$1" -tAc "SELECT count(*) FROM $table")"
  done
}

WEBAPI_STOPPED=false
start_webapi() {
  if [ "$WEBAPI_STOPPED" = true ]; then
    docker start "$WEBAPI" >/dev/null && echo "Started $WEBAPI"
  fi
}
trap start_webapi EXIT

if [ "$TARGET" = "$DB" ] && [ "$YES" = false ]; then
  echo "This replaces every row in database '$DB' with $FILE."
  read -r -p "Type the database name ($DB) to continue: " answer
  [ "$answer" = "$DB" ] || { echo "Cancelled."; exit 1; }
fi

if [ "$TARGET" = "$DB" ]; then
  TEMP="${DB}_restore_tmp"
  # Restore into a temp database first, so a broken dump leaves Data untouched
  restore_into "$TEMP"
  if [ "$(docker inspect -f '{{.State.Running}}' "$WEBAPI" 2>/dev/null)" = true ]; then
    docker stop "$WEBAPI" >/dev/null && WEBAPI_STOPPED=true && echo "Stopped $WEBAPI"
  fi
  psql_admin "DROP DATABASE IF EXISTS \"$DB\" WITH (FORCE);"
  psql_admin "ALTER DATABASE \"$TEMP\" RENAME TO \"$DB\";"
else
  restore_into "$TARGET"
fi

echo "Restored $FILE into $TARGET. Row counts:"
print_counts "$TARGET"
