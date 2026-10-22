#!/usr/bin/env bash
# Restores a backup made by db-backup.sh from ./backup.
# Usage: scripts/db-restore.sh [<file>] [--yes] [--target <db>]
#   <file>         a file name in ./backup or a path; without it, lists the backups in ./backup
#                  and asks which one to restore (with --yes, takes the newest)
#   default        restores into Data: asks for confirmation, restores into a temp database,
#                  and only if that succeeds stops webapi, backs up the current Data, swaps the
#                  temp database in, applies newer migrations and starts webapi again
#   --target <db>  restores into a scratch database and leaves Data and webapi alone; asks
#                  first if that database already exists
#   --yes          skips the questions
set -euo pipefail

CONTAINER=${CONTAINER:-billio-db}
WEBAPI=${WEBAPI:-billio-webapi}
LIQUIBASE=${LIQUIBASE:-billio-liquibase}
DB=${DB:-Data}
BACKUP_DIR=${BACKUP_DIR:-backup}
TEMP="${DB}_restore_tmp"
OLD="${DB}_restore_old"
SCRIPT_DIR=$(dirname "$0")
TABLES="users sellers customers items invoices"

usage() { echo "Usage: $0 [<file>] [--yes] [--target <db>]" >&2; exit 1; }

FILE=""
TARGET="$DB"
YES=false
while [ $# -gt 0 ]; do
  case "$1" in
    --yes) YES=true; shift ;;
    --target) [ $# -ge 2 ] || usage; TARGET="$2"; shift 2
      case "$TARGET" in "$TEMP"|"$OLD") echo "$TARGET is used by the restore itself" >&2; exit 1 ;; esac ;;
    -*) usage ;;
    *) [ -z "$FILE" ] || usage; FILE="$1"; shift ;;
  esac
done
if [ -z "$FILE" ]; then
  # Names hold the timestamp, so sort by name, not by file time (a copied file gets a new one)
  mapfile -t BACKUPS < <(ls -1 "$BACKUP_DIR"/billio-*.dump 2>/dev/null | sort -r)
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
db_exists() { [ "$(docker exec "$CONTAINER" psql -U postgres -d postgres -tAc "SELECT 1 FROM pg_database WHERE datname = '$1'")" = 1 ]; }
# True only when the count query answers 0, so a failed query never skips the backup
db_is_empty() { [ "$(docker exec "$CONTAINER" psql -U postgres -d "$1" -tAc "SELECT count(*) FROM pg_tables WHERE schemaname = 'public'")" = 0 ]; }

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

# webapi and the liquibase container both use Data, so only a restore into Data touches them
LIVE=false
[ "$DB" = Data ] && LIVE=true
WEBAPI_STOPPED=false
# True from the swap until Liquibase has run: webapi must not start on an unmigrated Data
MIGRATION_PENDING=false
on_exit() {
  if [ "$MIGRATION_PENDING" = true ]; then
    echo "$DB may lack newer migrations, so $WEBAPI was not started. The replaced database is kept" \
      "as $OLD. Fix Liquibase, run 'docker start -a $LIQUIBASE', then 'docker start $WEBAPI'" \
      "and drop $OLD." >&2
  elif [ "$WEBAPI_STOPPED" = true ]; then
    docker start "$WEBAPI" >/dev/null && echo "Started $WEBAPI"
  fi
}
trap on_exit EXIT

if [ "$YES" = false ] && { [ "$TARGET" = "$DB" ] || db_exists "$TARGET"; }; then
  echo "This replaces every row in database '$TARGET' with $FILE."
  read -r -p "Type the database name ($TARGET) to continue: " answer
  [ "$answer" = "$TARGET" ] || { echo "Cancelled."; exit 1; }
fi

if [ "$TARGET" = "$DB" ]; then
  # Restore into a temp database first, so a broken dump leaves Data untouched
  restore_into "$TEMP"
  if [ "$LIVE" = true ] && [ "$(docker inspect -f '{{.State.Running}}' "$WEBAPI" 2>/dev/null)" = true ]; then
    docker stop "$WEBAPI" >/dev/null && WEBAPI_STOPPED=true && echo "Stopped $WEBAPI"
  fi
  if db_exists "$DB"; then
    if ! db_is_empty "$DB"; then
      # Back up what is being replaced, so picking the wrong file can be undone. KEEP=0: no
      # rotation, which could delete the file being restored.
      CONTAINER="$CONTAINER" DB="$DB" BACKUP_DIR="$BACKUP_DIR" KEEP=0 BACKUP_SUFFIX=-before-restore \
        bash "$SCRIPT_DIR/db-backup.sh"
    fi
    # Both renames run in one transaction, so there is always a database called Data
    psql_admin "DROP DATABASE IF EXISTS \"$OLD\" WITH (FORCE);"
    psql_admin "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname IN ('$DB', '$TEMP') AND pid <> pg_backend_pid();
      ALTER DATABASE \"$DB\" RENAME TO \"$OLD\";
      ALTER DATABASE \"$TEMP\" RENAME TO \"$DB\";" >/dev/null
  else
    psql_admin "ALTER DATABASE \"$TEMP\" RENAME TO \"$DB\";"
  fi
  if [ "$LIVE" = true ]; then
    # An older backup lacks the newer migrations: apply them before webapi starts again
    MIGRATION_PENDING=true
    docker start -a "$LIQUIBASE" && MIGRATION_PENDING=false
  else
    echo "Migrations not applied: the $LIQUIBASE container only targets Data." >&2
  fi
  # Keep the replaced database while migrations are pending, so it can be renamed back
  if [ "$MIGRATION_PENDING" = false ]; then
    psql_admin "DROP DATABASE IF EXISTS \"$OLD\" WITH (FORCE);"
  fi
else
  restore_into "$TARGET"
fi

echo "Restored $FILE into $TARGET. Row counts:"
print_counts "$TARGET"

[ "$MIGRATION_PENDING" = false ]
