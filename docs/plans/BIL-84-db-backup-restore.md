# Plan: database backup and restore script

Jira: [BIL-84](https://aldisadom.atlassian.net/browse/BIL-84). Related: BIL-82 (production on the small PC). The series plan ([BIL-65-series.md](BIL-65-series.md)) needs a backup before migration 033.

## Goal

- One command saves the whole Billio database to a file.
- One command restores a file into the database.
- The README lists the raw commands too, so a restore works even without the scripts.
- A backup can be checked without touching the live database.

## Current state (master @ 125ae5e)

| Where | What |
|---|---|
| `docker-compose.yml` | `db` service, container `billio-db`, `postgres:16`, database `Data`, user/password `postgres`, volume `postgres_data`, host port 5437 |
| `makefile` | `run` and `update` targets only |
| Backups | None. No script and nothing in the README |
| Liquibase | Its tables (`databasechangelog`, `databasechangeloglock`) live in the same database, so a full dump also records which migrations have run |

## Design decisions

1. **`pg_dump -Fc` (custom format).** It is compressed, and `pg_restore` can restore it selectively and list its contents for checking.
2. **Dump inside the container, then `docker cp` it out.** The production PC runs Linux, but development is on Windows/WSL, where piping `docker exec ... > file` through PowerShell breaks binary output. Writing to `/tmp` inside the container and copying the file out works everywhere.
3. **Restore = drop and recreate `Data`, then `pg_restore`.** This is cleaner than `--clean` on a live database, which leaves anything not in the dump behind. `webapi` is stopped during the restore so it doesn't hold connections.
4. **Restoring an older backup is fine.** The dump carries `databasechangelog`, so `docker compose up liquibase` afterwards applies only the newer migrations.
5. **Backups go to `backups/` (git-ignored)** as `billio-YYYYMMDD-HHMMSS.dump`. The script keeps the newest 14 by default (`KEEP=n` to change it).
6. **Backups are run by hand.** No scheduling. Run `make backup` before every `make update` and before any migration that changes or drops data.
7. **Restore asks for confirmation** (type the database name). `--yes` skips the prompt for scripted use.

## Step 1: Scripts (`scripts/`)

`scripts/db-backup.sh`
```bash
#!/usr/bin/env bash
set -euo pipefail
CONTAINER=${CONTAINER:-billio-db}; DB=${DB:-Data}; KEEP=${KEEP:-14}
OUT_DIR=${OUT_DIR:-backups}; NAME="billio-$(date +%Y%m%d-%H%M%S).dump"
mkdir -p "$OUT_DIR"
docker exec "$CONTAINER" pg_dump -U postgres -d "$DB" -Fc -f "/tmp/$NAME"
docker cp "$CONTAINER:/tmp/$NAME" "$OUT_DIR/$NAME"
docker exec "$CONTAINER" rm "/tmp/$NAME"
# check: the archive must be readable and list at least one table
docker exec -i "$CONTAINER" pg_restore --list < "$OUT_DIR/$NAME" | grep -q "TABLE DATA" \
  || { echo "Backup check failed: $OUT_DIR/$NAME"; exit 1; }
ls -1t "$OUT_DIR"/billio-*.dump | tail -n +$((KEEP + 1)) | xargs -r rm --
echo "Saved $OUT_DIR/$NAME ($(du -h "$OUT_DIR/$NAME" | cut -f1))"
```

`scripts/db-restore.sh <file> [--yes] [--target <db>]`
- Without `--target`, restores into `Data`: asks for confirmation, `docker compose stop webapi`, drops and recreates `Data`, restores, `docker compose start webapi`.
- With `--target Data_restore_test`, restores into a scratch database and leaves `Data` and `webapi` alone. Use this to check a backup.
- Prints row counts for `users`, `sellers`, `customers`, `items`, `invoices` after the restore.

Core commands:
```bash
docker cp "$FILE" billio-db:/tmp/restore.dump
docker exec billio-db psql -U postgres -d postgres -c "DROP DATABASE IF EXISTS \"$TARGET\" WITH (FORCE);"
docker exec billio-db psql -U postgres -d postgres -c "CREATE DATABASE \"$TARGET\";"
docker exec billio-db pg_restore -U postgres -d "$TARGET" --no-owner --exit-on-error /tmp/restore.dump
docker exec billio-db rm /tmp/restore.dump
```

## Step 2: makefile and .gitignore

```make
backup:
	bash scripts/db-backup.sh

restore:
	bash scripts/db-restore.sh $(FILE)
```
Add both to `.PHONY`. Add `backups/` to `.gitignore`.

The existing `update` target runs `git reset --hard` and then applies new migrations. Backups stay manual, so `update` doesn't call `backup`. Instead it prints a one-line reminder first: `@echo "Did you run 'make backup'?"`.

## Step 3: README ("Backup and restore" section, in `README.md` and `docs/README_en.md`)

- `make backup` / `make restore FILE=backups/billio-....dump`.
- Raw commands (`docker exec` / `docker cp` lines, no shell redirection), so they also work from PowerShell on the dev machine.
- How to check a backup: restore into `Data_restore_test`, compare counts, then `DROP DATABASE Data_restore_test`.
- After restoring an older backup: `docker compose up liquibase` to apply newer migrations.
- When to back up: before `make update`, and before deploying a migration that changes or drops data.
- Keep a copy off the PC (USB drive or cloud folder). A backup on the same disk doesn't survive a disk failure.

## Step 4: Manual checks

- [ ] `make backup` creates a file and the check passes.
- [ ] `make restore FILE=... ` with `--target Data_restore_test`: counts match the live database.
- [ ] Full restore into `Data` on the local stack: the API starts and an old invoice PDF generates.
- [ ] Restore a backup taken before the newest migration, run liquibase, and confirm it applies only the missing changesets.
- [ ] Run `make backup` and a scratch restore on the production PC (Linux).

## Out of scope

- Scheduled backups. Backups are manual.
- Point-in-time recovery (WAL archiving). Overkill for this data volume.
- Encrypting backups. Worth adding if copies go to cloud storage, since `users` holds password hashes.

## Order of work

1. Scripts and makefile targets, tested on the local stack.
2. README section.
3. Run it on the production PC and take the backup that the series migration (BIL-65) needs.
